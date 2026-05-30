# ThePantry

A completely vibe coded/AI slopped Blazor Server application for managing your home pantry, inventory, and shopping lists- all without unit tests. It features UPC scanning integration with OpenFoodFacts for easy item entry. Originally vibesloped by [tjacoby2006](https://github.com/tjacoby2006) and reslopped further by [jzqos](https://github.com/jzqos).

## Features

- **Inventory Management**: Track items, quantities, and expiration dates.
- **UPC & QR Scanning**: Scan barcodes and QR codes from your phone (with a satisfying ding) and integrate with OpenFoodFacts to automatically fetch product details.
- **Shopping List**: Automatically track low-stock items and mark them as purchased.
- **Scan Confirmation**: After each scan an OK / Wrong item — retry card appears so you can confirm or undo the scan before it processes.
- **Scan Monitor**: Background processing of scanned items, including a Label Recognition tab for AI-processed items.
- **Dashboard**: Quick overview of your pantry status.
- **Combine Products**: Buying different brands of the same product? Combine them under a single name with multiple SKUs.
- **Storage Locations**: Assign items to named locations (Refrigerator, Freezer, Pantry, or custom ones like "Pantry Cellar"). Add and remove locations in Settings. All dropdowns throughout the app update dynamically.
- **Bulk Move**: Select multiple inventory items and move them to a different location in one action.
- **Freezer Warning**: Freezer items get a ⚠️ indicator and sort to the top of the inventory list so you don't forget about them.
- **Open One at a Time**: Marking an item as "opened" removes one sealed unit and adds a new opened unit — the rest of the stack stays sealed.
- **AI Label Recognition**: When a barcode isn't found in OpenFoodFacts (common for store-reduced / clearance items), the app automatically photographs the label and sends it to an LLM to extract the product name, animal species, and weight. A second "front label" photo can be captured manually to give the AI more context. The photo is deleted immediately after processing. Supports **Anthropic**, **OpenAI**, **OpenRouter**, **Ollama**, and **llama.cpp**.

## Tech Stack

- **Frontend**: Blazor Server (.NET 10)
- **Database**: Entity Framework Core with SQLite
- **Patterns**: MediatR for CQRS (Commands/Queries)
- **Background Tasks**: Hosted Services for scan processing and label recognition
- **External APIs**: OpenFoodFacts (product lookup), configurable LLM (label recognition)

---

## Docker Deployment

A ready-to-use `docker-compose.yml` is included in the repository root.

1. Clone the repository:
   ```bash
   git clone https://github.com/jzqos/ThePantry ThePantry
   cd ThePantry
   ```
2. Copy and edit the environment file:
   ```bash
   cp .env.example .env   # or create .env manually — see Configuration below
   ```
3. Build and start:
   ```bash
   docker compose up -d --build
   ```
4. Open your browser to `http://localhost:8999`.

The app creates the SQLite database and applies all migrations automatically on first start. All data is persisted in `./data/`.

### Updating

```bash
cd ThePantry
git pull
docker compose up -d --build
```

---

## Configuration

All user-facing settings live in the `.env` file next to `docker-compose.yml`. Create it by copying the example or writing it from scratch:

```env
# ── Authentication ────────────────────────────────────────────────────────────
# Password required to log in to the app. Change this before exposing the app
# to the internet or a shared network.
PASSWORD=yourpassword

# ── Timezone ──────────────────────────────────────────────────────────────────
# IANA timezone name for the server. Controls how timestamps are displayed.
# Find your name at: https://en.wikipedia.org/wiki/List_of_tz_database_time_zones
# Examples: Europe/Zurich, Europe/Berlin, America/New_York, Asia/Tokyo
TZ=Europe/Zurich

# ── OpenFoodFacts API ─────────────────────────────────────────────────────────
# Base URL for the OpenFoodFacts product lookup. The default points to the
# global instance. You can switch to a regional mirror if needed.
OPENFOODFACTS_ADDRESS=https://world.openfoodfacts.net/
```

### docker-compose.yml — what can be changed

```yaml
ports:
  - "8999:8080"   # Change 8999 to any port you prefer on your host machine.
                  # The right side (8080) is fixed inside the container.
```

All other values in `docker-compose.yml` are read from `.env` automatically. You should not need to edit `docker-compose.yml` directly for normal use.

---

## In-App Settings

The following are configured inside the app itself (Settings page), stored in the database:

### Storage Locations
The three default locations (Pantry, Refrigerator, Freezer) are always available. You can add custom ones (e.g. "Pantry Cellar", "Pantry Apartment") and remove any you've added. All scan and inventory dropdowns update immediately.

### Label Recognition (AI)
When a barcode is not found in OpenFoodFacts — common for store-reduced or clearance meat — the app can photograph the label and use an AI model to recognise the product name, species, and weight.

1. Open **Settings → Label Recognition**.
2. Select your LLM provider from the dropdown.
3. Enter your API key (not required for Ollama / llama.cpp).
4. Optionally set a specific model name and endpoint URL.
5. Save. Label recognition activates immediately for the next unknown barcode.

| Provider | API key needed | Default model | Notes |
|---|---|---|---|
| **Anthropic** | Yes | `claude-haiku-4-5-20251001` | Paid API, fast and cheap |
| **OpenAI** | Yes | `gpt-4o-mini` | Any vision-capable model works |
| **OpenRouter** | Yes | `meta-llama/llama-3.2-11b-vision-instruct:free` | Many free vision models available — see [openrouter.ai/models](https://openrouter.ai/models) |
| **Ollama** | No | `llava` | Self-hosted; needs a vision model loaded (e.g. `llava`, `llava-phi3`, `moondream`) |
| **llama.cpp** | Optional | *(auto)* | Self-hosted server; load a GGUF vision model before starting |

### Database Backup
Go to **Settings → Data Management → Download Backup** to download a copy of the SQLite database. To restore, replace `./data/thepantry.db` with your backup and restart the container.
