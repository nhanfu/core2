import { createClient } from "https://esm.sh/@supabase/supabase-js@2.44.2";
import { HonoContext } from "https://deno.land/x/hono@v4.3.11/mod.ts";

// Initialize Supabase Client
// Deno.env.get() will read the environment variables you set in Deno Deploy.
const supabaseUrl = Deno.env.get("SUPABASE_URL")!;
const supabaseKey = Deno.env.get("SUPABASE_KEY")!;
const supabase = createClient(supabaseUrl, supabaseKey);

/**
 * Saves a JSON file to Supabase Storage.
 * This is the core logic for the /api/save endpoint.
 */
export const saveJsonHandler = async (c: HonoContext) => {
  try {
    // Parse the incoming JSON body from the request.
    const { filename, fileContent } = await c.req.json();
    console.error("Supabase upload:", filename, fileContent);

    if (!filename || !fileContent) {
      return c.json({ error: "Filename and fileContent are required." }, 400);
    }

    // The actual upload logic
    const { data, error } = await supabase.storage
      .from("nhanjs") // Your bucket name
      .upload(filename, fileContent, {
        contentType: "application/json;charset=UTF-8",
        upsert: true, // Overwrite if the file exists
      });

    if (error) {
      // If Supabase returns an error (like RLS issue), throw it.
      console.error("Supabase upload error:", error);
      throw new Error(error.message);
    }

    console.log("Upload successful, data:", data);
    return c.json({ success: true, data: data }, 200);

  } catch (err) {
    console.error("Error in saveJsonHandler:", err);
    return c.json({ error: "Internal Server Error", message: err.message }, 500);
  }
};

/**
 * A simple handler for a GET request to greet a user.
 * Demonstrates how to handle URL parameters.
 */
export const greetHandler = (c: HonoContext) => {
    const name = c.req.param('name') || 'World';
    return c.text(`Hello, ${name}! The time is ${new Date().toLocaleTimeString('en-VN', { timeZone: 'Asia/Ho_Chi_Minh' })}.`);
};
