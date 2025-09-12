# Deno Supabase Serverless

This project is a serverless application built with Deno that allows users to save JSON files to Supabase storage. It provides a simple API endpoint to handle the upload of JSON files.

## Project Structure

```
deno-supabase-serverless
├── src
│   ├── api
│   │   └── save_json.ts
│   └── types
│       └── index.ts
├── deps.ts
├── deno.json
└── README.md
```

## Setup Instructions

1. **Clone the repository:**
   ```bash
   git clone https://github.com/yourusername/deno-supabase-serverless.git
   cd deno-supabase-serverless
   ```

2. **Install Deno:**
   Follow the instructions on the [Deno website](https://deno.land/) to install Deno on your machine.

3. **Configure Supabase:**
   - Create a Supabase account and project at [Supabase.io](https://supabase.io/).
   - Obtain your Supabase URL and API key from the project settings.

4. **Set environment variables:**
   Create a `.env` file in the root directory and add your Supabase credentials:
   ```
   SUPABASE_URL=your_supabase_url
   SUPABASE_ANON_KEY=your_supabase_anon_key
   ```

## API Usage

### Save JSON File

**Endpoint:** `POST /api/save_json`

**Request Body:**
```json
{
  "filename": "example.json",
  "fileContent": {
    "key": "value"
  }
}
```

**Response:**
- On success: Returns a success message and the URL of the uploaded file.
- On error: Returns an error message.

## License

This project is licensed under the MIT License. See the LICENSE file for more details.