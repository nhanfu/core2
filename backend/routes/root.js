'use strict'

import { LRUCache } from 'lru-cache';

// LRU Cache configuration
const serviceCache = new LRUCache({
  max: 100, // Maximum number of items in the cache
  ttl: 300000, // Time-to-live for each item (5 minutes in milliseconds)
});

const getCachedServiceInstance = async (path) => {
  // Check if the service instance exists in the cache
  if (serviceCache.has(path)) {
    return serviceCache.get(path);
  }

  // Dynamically import and create a new service instance
  const ServiceClass = (await import('../services/' + path)).default;
  if (!ServiceClass) {
    throw new Error(`Service class not found in path: ${path}`);
  }

  const instance = new ServiceClass();

  // Add the new instance to the cache
  serviceCache.set(path, instance);
  return instance;
};

const invokeService = async (path, action, parameters) => {
  try {
    // Get the cached service instance or create a new one
    const serviceInstance = await getCachedServiceInstance(path);

    if (typeof serviceInstance[action] !== 'function') {
      throw new Error(`Action "${action}" is not a function in the service class`);
    }

    // Call the action with the provided parameters
    return await serviceInstance[action](parameters);
  } catch (error) {
    console.error(`Error invoking service: ${error.message}`);
    throw error;
  }
};


export default async function (fastify, opts) {
  fastify.get('/', async function (request, reply) {
    return { root: true }
  });

  fastify.post('/service', async function (request, reply) {
    try {
      // Extract parameters from both query and body
      const { path, action, parameters: queryParameters } = request.query; // From query
      const { path: bodyPath, action: bodyAction, parameters: bodyParameters } = request.body || {}; // From body

      // Determine the source of parameters (query takes precedence)
      const servicePath = path || bodyPath;
      const serviceAction = action || bodyAction;
      const serviceParameters = queryParameters
        ? JSON.parse(queryParameters)
        : bodyParameters || {};

      // Validate required parameters
      if (!servicePath || !serviceAction) {
        return reply.status(400).send({ error: 'Missing required parameters: path or action' });
      }

      // Invoke the service
      const result = await invokeService(servicePath, serviceAction, serviceParameters);
      return reply.send(result); // Send the result back to the client
    } catch (error) {
      return reply.status(500).send({ error: error.message });
    }
  });
}

