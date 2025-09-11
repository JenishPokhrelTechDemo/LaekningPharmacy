import azure.functions as func  # Azure Functions SDK
import json
import os
import logging
import pyodbc
from openai import AzureOpenAI  # Azure OpenAI SDK
from azure.identity import DefaultAzureCredential  # Authentication helper
from azure.keyvault.secrets import SecretClient  # Access Key Vault secrets

# -----------------------
# DB Helper
# -----------------------
def get_all_products_from_db():
    """
    Fetches all products from the database with names and categories.
    """
    try:
        conn_str = SQL_CONN_STRING  # Store in Azure Function App settings
        with pyodbc.connect(conn_str) as conn:
            cursor = conn.cursor()
            cursor.execute("SELECT Name, Category FROM Products")
            return [{"name": row[0], "category": row[1]} for row in cursor.fetchall()]
    except Exception as e:
        logging.error(f"DB error: {str(e)}")
        return []

# -----------------------
# Azure Function App
# -----------------------
app = func.FunctionApp()  # Initialize Function App

def get_secret(name: str):
    """
    Retrieves a secret from Azure Key Vault using DefaultAzureCredential.
    """
    kv_uri = os.getenv("KEYVAULT_URI")
    cred = DefaultAzureCredential()
    client = SecretClient(vault_url=kv_uri, credential=cred)
    return client.get_secret(name).value

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
    Receives POST request with purchased products and returns 3-5 recommended products.
    Supports both:
      - old mode: allDbProductNames (list of names only)
      - new mode: allDbProducts (list of {name, category})
    """
    try:
        # Lazy load secrets
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
    
        # Parse request body
        req_body = req.get_json()
        purchased = req_body.get("purchasedProducts", [])
        all_products = req_body.get("allDbProducts")  # new mode
        all_product_names = req_body.get("allDbProductNames")  # old mode

        # Determine mode
        if all_products is None and all_product_names is not None:
            # Old mode
            all_products = [{"name": n, "category": None} for n in all_product_names]

        if not all_products:
            all_products = get_all_products_from_db()

        if not purchased or not all_products:
            return func.HttpResponse(
                json.dumps({"recommendedProducts": []}),
                status_code=200,
                mimetype="application/json"
            )

        # Build prompt
        if all_products and all_products[0].get("category") is not None:
            # Category-aware mode
            purchased_str = "; ".join(
                [f"{p} (Category: {next((ap['category'] for ap in all_products if ap['name'] == p), 'Unknown')})"
                 for p in purchased]
            )
            all_products_str = "; ".join([f"{p['name']} (Category: {p['category']})" for p in all_products])
            prompt = (
                f"You are a pharmacy assistant. A user has previously purchased these products: {purchased_str}. "
                f"From the following available products: {all_products_str}, recommend 3–5 products that belong "
                f"to the same exact categories as the purchased products. "
                f"Only return exact product names from the provided list, separated by commas."
            )
        else:
            # Name-only mode (fallback)
            purchased_str = ", ".join(purchased)
            all_products_str = ", ".join([p["name"] for p in all_products])
            prompt = (
                f"You are a pharmacy assistant. A user has previously purchased these products: {purchased_str}. "
                f"From the following available products: {all_products_str}, recommend 3–5 products that are in "
                f"similar categories based on product names. "
                f"Only return exact product names from the provided list, separated by commas."
            )

        logging.info(f"Prompt sent to Azure OpenAI: {prompt}")

        # Call Azure OpenAI
        response = client.chat.completions.create(
            model=AZURE_OPENAI_DEPLOYMENT,
            messages=[{"role": "system", "content": prompt}]
        )

        result_text = response.choices[0].message.content if response.choices else ""
        logging.info(f"Raw model output: {result_text}")

        # Normalize and filter
        normalized_products = [p["name"].lower().strip() for p in all_products]
        recommended_products = []

        for rec in result_text.replace("\n", ",").split(","):
            rec_norm = rec.lower().strip()
            for idx, p in enumerate(normalized_products):
                if rec_norm == p:
                    recommended_products.append(all_products[idx]["name"])
                    break

        logging.info(f"Filtered recommended products: {recommended_products}")

        return func.HttpResponse(
            json.dumps({"recommendedProducts": recommended_products}),
            status_code=200,
            mimetype="application/json"
        )

    except Exception as e:
        logging.error(f"Exception in recommend function: {str(e)}")
        return func.HttpResponse(
            json.dumps({"error": str(e)}),
            status_code=500,
            mimetype="application/json"
        )
