# ThePantry

A completely vibe coded/AI slopped Blazor Server application for managing your home pantry, inventory, and shopping lists- all without unit tests. It features UPC scanning integration with OpenFoodFacts for easy item entry. Originally vibesloped by [tjacoby2006](https://github.com/tjacoby2006) and reslopped further by [jzqos](https://github.com/jzqos).

## Features

- **Inventory Management**: Track items, quantities, and expiration dates.
- **UPC Scanning**: Scan UPCs from your phone (with a satisfying ding) and integrate with OpenFoodFacts to automatically fetch product details.
- **Shopping List**: Automatically track low-stock items and mark them as purchased.
- **Scan Monitor**: Background processing of scanned items, including a Label Recognition tab for AI-processed items.
- **Dashboard**: Quick overview of your pantry status.
- **Combine Products**: Buying different brands of the same product? Combine them under a single name with multiple SKUs.
- **Storage Locations**: Assign items to named locations (Refrigerator, Freezer, Pantry, or custom ones like "Pantry Cellar"). Add and remove locations in Settings. All dropdowns throughout the app update dynamically.
- **Bulk Move**: Select multiple inventory items and move them to a different location in one action.
- **Extended Shelf Life Warning**: Items with a shelf life over 30 days (e.g. frozen meat) get a ⚠️ indicator and sort to the top of the inventory list so you don't forget about them.
- **AI Label Recognition**: When a barcode isn't found in OpenFoodFacts (common for store-reduced / clearance items), the app automatically photographs the label and sends it to an LLM to extract the product name, animal species, and weight. Supports **Anthropic**, **OpenAI**, **OpenRouter**, **Ollama**, and **llama.cpp**. The photo is deleted immediately after processing.
  - A second "front label" photo can be captured manually to give the AI more information.
  - Items created via label recognition receive a 90-day shelf life and a 2-day use-after-open default, and appear with the ⚠️ warning as a reminder.

## Tech Stack

- **Frontend**: Blazor Server (.NET 10)
- **Database**: Entity Framework Core with SQLite
- **Patterns**: MediatR for CQRS (Commands/Queries)
- **Background Tasks**: Hosted Services for scan processing and label recognition
- **External APIs**: OpenFoodFacts (product lookup), configurable LLM (label recognition)

## Docker Deployment

A ready-to-use `docker-compose.yml` is included in the repository root.

1. Clone the repository into your target Docker folder:
   ```bash
   git clone https://github.com/jzqos/ThePantry ThePantry
   cd ThePantry
   ```
2. Set your password in a `.env` file (or pass it inline):
   ```bash
   echo "PASSWORD=yourpassword" > .env
   ```
3. Build and start:
   ```bash
   docker compose up -d --build
   ```
4. Open your browser to `http://localhost:8999`.

The app creates the SQLite database and applies all migrations automatically on first start. Data is persisted in `./data/`.

### Updating

```bash
cd ThePantry
git pull
docker compose up -d --build
```

### Configuring Label Recognition

1. Open Settings in the app.
2. Under **Label Recognition**, select your LLM provider.
3. Enter your API key (not needed for Ollama / llama.cpp).
4. Optionally override the model name and endpoint URL.
5. Save — label recognition activates immediately for the next unknown barcode.

Supported providers:

| Provider | Notes |
|---|---|
| **Anthropic** | Claude Haiku recommended (`claude-haiku-4-5-20251001`) |
| **OpenAI** | `gpt-4o-mini` or newer vision-capable models |
| **OpenRouter** | Many free vision models available — see openrouter.ai/models |
| **Ollama** | Self-hosted; use a vision model like `llava` or `llava-phi3` |
| **llama.cpp** | Self-hosted server with a GGUF vision model loaded |
