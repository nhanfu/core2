import express from 'express';
import setRoutes from './routes/index.js';
import cors from 'cors';
import morgan from 'morgan';
import winston from 'winston';

const app = express();
const PORT = process.env.PORT || 3000;

// Winston logger setup
const logger = winston.createLogger({
    level: 'info',
    format: winston.format.combine(
        winston.format.timestamp(),
        winston.format.json()
    ),
    transports: [
        new winston.transports.Console(),
        // You can add file transports here if needed
    ],
});

// Morgan setup to use winston for HTTP logs
app.use(morgan('combined', {
    stream: {
        write: (message) => logger.info(message.trim())
    }
}));

app.use(express.json());
app.use(express.urlencoded({ extended: true }));
app.use(cors());

// Health check route
app.get('/health', (req, res) => res.status(200).send('OK'));

// Initialize routes
setRoutes(app);

// Error handling middleware
app.use((err, req, res, next) => {
    logger.error('Error:', { message: err.message, stack: err.stack });
    res.status(500).json({ error: 'Internal Server Error' });
});

// Graceful shutdown
const server = app.listen(PORT, () => {
    logger.info(`Server is running on http://localhost:${PORT}`);
});

process.on('SIGTERM', () => {
    server.close(() => {
        logger.info('Process terminated');
    });
});