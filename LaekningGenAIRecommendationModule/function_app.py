import azure.functions as func
import json
import os
import logging
from openai import AzureOpenAI
from azure.identity import DefaultAzureCredential
from azure.keyvault.secrets import SecretClient
import pyodbc  # ensure pyodbc is imported for DB helper

# -----------------------
# DB Helper
# -----------------------
def get_all_products_from_db():
    try:
        conn_str = SQL_CONN_STRING
        with pyodbc.connect(conn_str) as conn:
            cursor = conn.cursor()
            cursor.execute("SELECT Name, Category FROM Products")  # include category
            return [{"name": row[0], "category": row[1]} for row in cursor.fetchall()]
    except Exception as e:
        logging.error(f"DB error: {str(e)}")
        return []

# -----------------------
# Azure Function App
# -----------------------
app = func.FunctionApp()

def get_secret(name: str):
    kv_uri = os.getenv("KEYVAULT_URI")
    cred = DefaultAzureCredential()
    client = SecretClient(vault_url=kv_uri, credential=cred)
    return client.get_secret(name).value

@app.function_name(name="recommend")
@app.route(route="recommend", auth_level=func.AuthLevel.FUNCTION, methods=["POST"])
def recommend(req: func.HttpRequest) -> func.HttpResponse:
    try:
        # Load Azure OpenAI secrets
        AZURE_OPENAI_KEY = get_secret("AzureOpenAIDeploymentKeyTwo")
        AZURE_OPENAI_ENDPOINT = get_secret("AzureOpenAIEndpoint")
        AZURE_OPENAI_DEPLOYMENT = get_secret("AzureOpenAIDeploymentName")
        AZURE_OPENAI_API_VERSION = get_secret("AzureOpenAIApiVersion")

        client = AzureOpenAI(
            api_key=AZURE_OPENAI_KEY,
            azure_endpoint=AZURE_OPENAI_ENDPOINT,
            api_version=AZURE_OPENAI_API_VERSION
        )

        req_body = req.get_json()
        purchased_categories = req_body.get("purchasedCategories", [])
        all_products = req_body.get("allProducts", [])

        # Fetch from DB if not provided
        if not all_products:
            all_products = get_all_products_from_db()

        if not purchased_categories or not all_products:
            return func.HttpResponse(
                json.dumps({"recommendedProducts": []}),
                status_code=200,
                mimetype="application/json"
            )

        # Filter products to purchased categories only
        filtered_products = [p for p in all_products if p["category"] in purchased_categories]
        if not filtered_products:
            return func.HttpResponse(
                json.dumps({"recommendedProducts": []}),
                status_code=200,
                mimetype="application/json"
            )

        # AI prompt using filtered products
        product_names_str = ", ".join([p["name"] for p in filtered_products])
        prompt = (
            f"You are a pharmacy assistant. Recommend 3–5 products from these categories: "
            f"{', '.join(purchased_categories)}. "
            f"Available products: {product_names_str}. "
            f"Return exact product names from the list, separated by commas, no commentary."
        )

        logging.info(f"Prompt sent to Azure OpenAI: {prompt}")

        response = client.chat.completions.create(
            model=AZURE_OPENAI_DEPLOYMENT,
            messages=[{"role": "system", "content": prompt}]
        )

        result_text = response.choices[0].message.content if response.choices else ""
        logging.info(f"Raw model output: {result_text}")

        # Map AI output to exact product names
        normalized_names = [p["name"].lower().strip() for p in filtered_products]
        recommended_products = []
        for rec in result_text.replace("\n", ",").split(","):
            rec_norm = rec.lower().strip()
            for idx, name in enumerate(normalized_names):
                if rec_norm == name:
                    recommended_products.append(filtered_products[idx]["name"])
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
