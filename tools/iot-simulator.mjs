// IoT simulator: sends weight readings like the real microcontroller (Node 18 or newer, no packages).
// Usage:  node tools/iot-simulator.mjs --sensor 3 --weight 12.36 --every 10
//   --api    API base URL            (default http://localhost:5000/api)
//   --sensor sensor id               (default 1)
//   --weight starting weight in kg   (default 2)
//   --every  seconds between readings (default 10)
//   --noise  kg of random variation  (default 0.05)
//   --key    value of the X-Device-Key header, if Devices:ApiKey is configured
// (Simulador IoT: envía lecturas de peso como el microcontrolador real.)

const args = Object.fromEntries(
    process.argv.slice(2).reduce((pairs, token, index, all) => {
        if (token.startsWith('--')) pairs.push([token.slice(2), all[index + 1]]);
        return pairs;
    }, [])
);

const api = args.api ?? 'http://localhost:5000/api';
const sensor = Number(args.sensor ?? 1);
const every = Number(args.every ?? 10);
const noise = Number(args.noise ?? 0.05);
let weight = Number(args.weight ?? 2);

async function sendReading() {
    const reading = Math.max(0, Math.round((weight + (Math.random() - 0.5) * 2 * noise) * 100) / 100);
    const headers = { 'Content-Type': 'application/json' };
    if (args.key) headers['X-Device-Key'] = args.key;

    try {
        const response = await fetch(`${api}/sensores/${sensor}/lecturas`, {
            method: 'POST',
            headers,
            body: JSON.stringify({ peso: reading }),
        });
        console.log(`${new Date().toLocaleTimeString()}  sensor ${sensor}  ${reading} kg  ->  ${response.status}`);
    } catch (error) {
        console.log(`Could not reach the API: ${error.message}`);
    }
}

console.log(`Simulating sensor ${sensor} every ${every}s on ${api}  (Ctrl+C to stop)`);
await sendReading();
setInterval(sendReading, every * 1000);