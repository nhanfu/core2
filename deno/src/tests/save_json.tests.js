// Replace with your actual API endpoint
const saveApi = "http://localhost:8000/api/saveJson"; // or your deployed endpoint

const payload = {
  filename: "example.json",
  fileContent: JSON.stringify({ hello: "world" }),
};

fetch(saveApi, {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(payload),
})
  .then(async (res) => {
    const text = await res.text();
    console.log("Status:", res.status);
    console.log("Response:", text);

    if (res.status !== 200) {
      throw new Error(`Expected 200, got ${res.status}: ${text}`);
    }
  })
  .catch((err) => {
    console.error("Error:", err);
  });

// Deno.test("POST /save_json uploads JSON file", async () => {
//   const res = await fetch(saevApi, {
//     method: "POST",
//     headers: { "Content-Type": "application/json" },
//     body: JSON.stringify(payload),
//   });

//   const text = await res.text();
//   console.log("Status:", res.status);
//   console.log("Response:", text);

//   if (res.status !== 200) {
//     throw new Error(`Expected 200, got ${res.status}: ${text}`);
//   }
// });

const getApiUrl = "http://localhost:8000/api/getJson?filename=accounting.json";

Deno.test("GET /get returns the saved JSON file", async () => {
  const res = await fetch(getApiUrl, {
    method: "GET",
    headers: { "Accept": "application/json" },
  });

  const text = await res.text();
  console.log("Status:", res.status);
  console.log("Response:", text);

  if (res.status !== 200) {
    throw new Error(`Expected 200, got ${res.status}: ${text}`);
  }
});