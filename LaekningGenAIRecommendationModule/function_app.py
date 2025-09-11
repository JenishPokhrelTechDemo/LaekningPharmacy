import azure.functions as func  # Azure Functions SDK
import json
import os
import logging
import pyodbc
import difflib
from openai import AzureOpenAI  # Azure OpenAI SDK
from azure.identity import DefaultAzureCredential  # Authentication helper
from azure.keyvault.secrets import SecretClient  # Access Key Vault secrets

# -----------------------
# DB Helper
# -----------------------
def get_all_products_from_db():
    """
    Fetches all products with their categories from the database.
    Uses pyodbc connection string stored in Azure Function App settings.
    Returns a list of dicts: [{"name": ..., "category": ...}, ...]
    """
    try:
        conn_str = os.getenv("SQL_CONN_STRING")  # Ensure set in Function App settings
        with pyodbc.connect(conn_str) as conn:
            cursor = conn.cursor()
            cursor.execute("SELECT Name, Category FROM Products")  # Adjust columns
            return [{"name": row[0], "category": row[1]} for row in cursor.fetchall()]
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
    from the same categories using Azure OpenAI.
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
        purchased = req_body.get("purchasedProducts", [])  # Expect list of product names
        all_products = req_body.get("allDbProducts", [])   # Expect [{"name":..., "category":...}, ...]

        # If the full product list isn't passed, fetch from DB
        if not all_products:
            all_products = get_all_products_from_db()

        # If inputs are empty, return empty recommendation
        if not purchased or not all_products:
            return func.HttpResponse(
                json.dumps({"recommendedProducts": []}),
                status_code=200,
                mimetype="application/json"
            )

        # Format purchased + all products with categories
        purchased_str = "; ".join([
            f"{p} (Category: {prod['category']})"
            for p in purchased
            for prod in all_products if prod["name"] == p
        ])

        all_products_str = "; ".join([
            f"{prod['name']} (Category: {prod['category']})"
            for prod in all_products
        ])

        # Construct the prompt for Azure OpenAI
        prompt = (
            f"You are a pharmacy assistant. A user has previously purchased these products: {purchased_str}. "
            f"Adding to and including {purchased_str}, from the following available products: {all_products_str}, "
            f"recommend 3–5 products that belong to the same exact categories as the purchased products. "
            f"Only return exact product names from the provided list, separated by commas. "
            f"Do not include commentary or explanations."
        )

        logging.info(f"Prompt sent to Azure OpenAI: {prompt}")

        # Call Azure OpenAI
        response = client.chat.completions.create(
            model=AZURE_OPENAI_DEPLOYMENT,
            messages=[{"role": "system", "content": prompt}]
        )

        # Extract the response text
        result_text = response.choices[0].message.content if response.choices else ""
        logging.info(f"Raw model output: {result_text}")

        # Normalize product names for exact/fuzzy matching
        normalized_products = [p["name"].lower().strip() for p in all_products]
        recommended_products = set()  # use set to avoid duplicates

        # Ensure purchased products are included
        for p in purchased:
            recommended_products.add(p)

        # Split AI output and filter matches
        for rec in result_text.replace("\n", ",").split(","):
            rec_norm = rec.lower().strip()
            if not rec_norm:
                continue

            # Try exact match first
            if rec_norm in normalized_products:
                idx = normalized_products.index(rec_norm)
                recommended_products.add(all_products[idx]["name"])
                continue

            # Fallback: fuzzy match (>= 0.85 similarity)
            closest = difflib.get_close_matches(rec_norm, normalized_products, n=1, cutoff=0.85)
            if closest:
                idx = normalized_products.index(closest[0])
                recommended_products.add(all_products[idx]["name"])

        recommended_products = list(recommended_products)

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
