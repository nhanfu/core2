export default function setRoutes(app) {
    app.get('/', (req, res) => res.send('Welcome to the Express backend!'));
}