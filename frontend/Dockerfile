FROM node:20-alpine

WORKDIR /app

COPY package.json ./

RUN npm install --include=dev

ENV PATH="/app/node_modules/.bin:$PATH"

EXPOSE 5173

# Use shell form so npm run dev launches vite in same PID (tini wrapper issue)
CMD npm run dev
