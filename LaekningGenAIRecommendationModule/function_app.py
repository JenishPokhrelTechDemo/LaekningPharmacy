import azure.functions as func
import json
import os
import logging
from openai import AzureOpenAI
from azure.identity import DefaultAzureCredential
from azure.keyvault.secrets import SecretClient

# -----------------------
# Load secrets from Azure Key Vault
# -----------------------
KV_URI = os.getenv("KEYVAULT_URI")
credential = DefaultAzureCredential()
secret_client = SecretClient(vault_url=KV_URI, credential=credential)

AZURE_OPENAI_KEY = secret_client.get_secret("AzureOpenAIDeploymentKeyTwo").value
AZURE_OPENAI_ENDPOINT = secret_client.get_secret("AzureOpenAIEndpoint").value
AZURE_OPENAI_DEPLOYMENT = secret_client.get_secret("AzureOpenAIDeploymentName").value  
AZURE_OPENAI_API_VERSION = secret_client.get_secret("AzureOpenAIAPIVersion").value
SQL_CONN_STRING = secret_client.get_secret("PythonSQLConnectionString").value

client = AzureOpenAI(
    api_key=AZURE_OPENAI_KEY,
    azure_endpoint=AZURE_OPENAI_ENDPOINT,
    api_version=AZURE_OPENAI_API_VERSION
)
# -----------------------
# DB Helper
# -----------------------
def get_all_products_from_db():
    try:
        conn_str=SQL_CONN_STRING  # Store in Azure Function App settings
        with pyodbc.connect(conn_str) as conn:
            cursor = conn.cursor()
            cursor.execute("SELECT Name FROM Products")  # adjust table/column
            return [row[0] for row in cursor.fetchall()]
    except Exception as e:
        logging.error(f"DB error: {str(e)}")
        return []

# -----------------------
# Azure Function App
# -----------------------
app = func.FunctionApp()

@app.function_name(name="recommend")
@app.route(route="recommend", auth_level=func.AuthLevel.FUNCTION, methods=["POST"])
def recommend(req: func.HttpRequest) -> func.HttpResponse:
    try:
        req_body = req.get_json()
        purchased = req_body.get("purchasedProducts", [])
        all_products = req_body.get("allDbProductNames", [])

        # If not passed, fetch from DB
        if not all_products:
            all_products = get_all_products_from_db()

        if not purchased or not all_products:
            return func.HttpResponse(
                json.dumps({"recommendedProducts": []}),
                status_code=200,
                mimetype="application/json"
            )

        purchased_str = ", ".join(purchased)
        all_products_str = ", ".join(all_products)

        prompt = (
            f"You are a pharmacy assistant. A user has previously purchased these products: {purchased_str}. "
            f"From the following available products: {all_products_str}, recommend 3–5 products that are in the same or related categories. "
            f"Only return exact product names from the provided list, separated by commas. Do not include commentary or explanations."
        )

        logging.info(f"Prompt sent to Azure OpenAI: {prompt}")

        # Call Azure OpenAI (non-streaming for simplicity)
        response = client.chat.completions.create(
            model=AZURE_OPENAI_DEPLOYMENT,
            messages=[{"role": "system", "content": prompt}]
        )

        result_text = response.choices[0].message.content if response.choices else ""
        logging.info(f"Raw model output: {result_text}")

        # Map model output to products (substring match)
        normalized_products = [p.lower().strip() for p in all_products]
        recommended_products = []
        for rec in result_text.replace("\n", ",").split(","):
            rec_norm = rec.lower().strip()
            for idx, p in enumerate(normalized_products):
                if rec_norm == p:  # exact match to avoid false positives
                    recommended_products.append(all_products[idx])
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


