import fs from 'fs/promises';
import path from 'path';

export default async function handler(req, res) {
  if (req.method !== 'GET') {
    return res.status(405).json({ error: 'Method not allowed' });
  }

  const { filename } = req.query;
  if (!filename) {
    return res.status(400).json({ error: 'Filename is required' });
  }

  const filePath = path.join(process.cwd(), filename);
  try {
    const content = await fs.readFile(filePath, 'utf-8');
    res.status(200).json({ content });
  } catch (err) {
    res.status(404).json({ error: 'File not found' });
  }
}
