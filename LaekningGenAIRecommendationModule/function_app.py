import azure.functions as func  # Azure Functions SDK
import json
import os
import logging
from openai import AzureOpenAI  # Azure OpenAI SDK
from azure.identity import DefaultAzureCredential  # Authentication helper
from azure.keyvault.secrets import SecretClient  # Access Key Vault secrets

# -----------------------
# DB Helper
# -----------------------
def get_all_products_from_db():
    """
    Fetches all product names from the database.
    Uses pyodbc connection string stored in Azure Function App settings.
    """
    try:
        conn_str = SQL_CONN_STRING  # Ensure this is set in Function App settings
        with pyodbc.connect(conn_str) as conn:
            cursor = conn.cursor()
            cursor.execute("SELECT Name FROM Products")  # Adjust table/column as needed
            return [row[0] for row in cursor.fetchall()]  # Return list of product names
    except Exception as e:
        logging.error(f"DB error: {str(e)}")
        return []  # Return empty list if DB fails

# -----------------------
# Azure Function App
# -----------------------
app = func.FunctionApp()  # Initialize Function App

def get_secret(name: str):
    """
    Retrieves a secret from Azure Key Vault using DefaultAzureCredential.
    """
    kv_uri = os.getenv("KEYVAULT_URI")  # Key Vault URL from environment variable
    cred = DefaultAzureCredential()  # Use managed identity or other default credentials
    client = SecretClient(vault_url=kv_uri, credential=cred)
    return client.get_secret(name).value  # Return the secret value

# -----------------------
# Main Function: Recommend Products
# -----------------------
@app.function_name(name="recommend")
@app.route(
    route="recommend",
    auth_level=func.AuthLevel.FUNCTION,
    methods=["POST"]
)
def recommend(req: func.HttpRequest) -> func.HttpResponse:
    """
    Receives POST request with purchased products and returns 3-5 recommended products
    using Azure OpenAI.
    """
    try:
        # Lazy load secrets from Key Vault
        AZURE_OPENAI_KEY = get_secret("AzureOpenAIDeploymentKeyTwo")
        AZURE_OPENAI_ENDPOINT = get_secret("AzureOpenAIEndpoint")
        AZURE_OPENAI_DEPLOYMENT = get_secret("AzureOpenAIDeploymentName")
        AZURE_OPENAI_API_VERSION = get_secret("AzureOpenAIApiVersion")

        # Initialize Azure OpenAI client
        client = AzureOpenAI(
            api_key=AZURE_OPENAI_KEY,
            azure_endpoint=AZURE_OPENAI_ENDPOINT,
            api_version=AZURE_OPENAI_API_VERSION
        )
    
        # Parse JSON request body
        req_body = req.get_json()
        purchased = req_body.get("purchasedProducts", [])
        all_products = req_body.get("allDbProductNames", [])
        
        # Extract purchased categories
        purchased_categories = {p['category'] for p in purchased}

        # Filter all products to only those in purchased categories
        all_products_filtered = [p for p in all_products if p['category'] in purchased_categories]
        
        # Build strings for AI prompt
        purchased_str = ", ".join([p['name'] for p in purchased])
        all_products_str = ", ".join([p['name'] for p in all_products_filtered])
        
        # Build strings for AI prompt
        purchased_str = ", ".join([p['name'] for p in purchased])
        all_products_str = ", ".join([p['name'] for p in all_products_filtered])

        prompt = (
            f"You are a pharmacy assistant. A user has previously purchased these products: {purchased_str}. "
            f"The categories of these products are: {', '.join(purchased_categories)}. "
            f"From the following available products: {all_products_str}, recommend 5 products, including last three items from {purchased_str} with that "
            f"that are in the same categories as the purchased items. "
            f"Only return exact product names from the provided list, separated by commas."
            f"Do not include commentary or explanations."
        )


        logging.info(f"Prompt sent to Azure OpenAI: {prompt}")

        # Call Azure OpenAI (non-streaming completion)
        response = client.chat.completions.create(
            model=AZURE_OPENAI_DEPLOYMENT,
            messages=[{"role": "system", "content": prompt}]
        )

        # Extract the response text
        result_text = response.choices[0].message.content if response.choices else ""
        logging.info(f"Raw model output: {result_text}")

        # Normalize product names for exact matching
        normalized_products = [p.lower().strip() for p in all_products]
        recommended_products = []

        # Split AI output and filter exact matches
        for rec in result_text.replace("\n", ",").split(","):
            rec_norm = rec.lower().strip()
            for idx, p in enumerate(normalized_products):
                if rec_norm == p:  # Ensure only exact matches are returned
                    recommended_products.append(all_products[idx])
                    break

        logging.info(f"Filtered recommended products: {recommended_products}")

        # Return recommended products as JSON
        return func.HttpResponse(
            json.dumps({"recommendedProducts": recommended_products}),
            status_code=200,
            mimetype="application/json"
        )

    except Exception as e:
        # Catch all exceptions and return HTTP 500
        logging.error(f"Exception in recommend function: {str(e)}")
        return func.HttpResponse(
            json.dumps({"error": str(e)}),
            status_code=500,
            mimetype="application/json"
        )
