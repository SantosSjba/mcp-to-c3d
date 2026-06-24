import { ApplicationClientConnection } from "./SocketClient.js";
import { createLogger } from "./logger.js";

const log = createLogger("ConnectionManager");

const CIVIL3D_HOST = process.env.CIVIL3D_HOST ?? "localhost";
const CIVIL3D_PORT = parseInt(process.env.CIVIL3D_PORT ?? "8080", 10);
const CONNECT_TIMEOUT_MS = parseInt(process.env.CIVIL3D_CONNECT_TIMEOUT ?? "5000", 10);

let sharedClient: ApplicationClientConnection | null = null;
let connectPromise: Promise<ApplicationClientConnection> | null = null;

function createClient(): ApplicationClientConnection {
  return new ApplicationClientConnection(CIVIL3D_HOST, CIVIL3D_PORT);
}

function connectClient(client: ApplicationClientConnection): Promise<void> {
  if (client.isConnected) {
    return Promise.resolve();
  }

  return new Promise<void>((resolve, reject) => {
    const onConnect = () => {
      cleanup();
      resolve();
    };

    const onError = () => {
      cleanup();
      log.error("Connection failed", { host: CIVIL3D_HOST, port: CIVIL3D_PORT });
      reject(
        new Error(
          `Failed to connect to Civil 3D plugin at ${CIVIL3D_HOST}:${CIVIL3D_PORT}. ` +
            `Make sure Civil 3D is running and the MCP plugin is loaded (NETLOAD).`
        )
      );
    };

    const onTimeout = () => {
      cleanup();
      log.warn("Connection timed out", {
        host: CIVIL3D_HOST,
        port: CIVIL3D_PORT,
        timeoutMs: CONNECT_TIMEOUT_MS,
      });
      reject(
        new Error(
          `Connection to Civil 3D plugin timed out after ${CONNECT_TIMEOUT_MS}ms. ` +
            `Verify the plugin is running on ${CIVIL3D_HOST}:${CIVIL3D_PORT}.`
        )
      );
    };

    const cleanup = () => {
      client.socket.removeListener("connect", onConnect);
      client.socket.removeListener("error", onError);
      clearTimeout(timeoutId);
    };

    client.socket.on("connect", onConnect);
    client.socket.on("error", onError);
    client.connect();

    const timeoutId = setTimeout(onTimeout, CONNECT_TIMEOUT_MS);
  });
}

/**
 * Returns a persistent shared connection, reconnecting automatically when needed.
 */
export async function getSharedConnection(): Promise<ApplicationClientConnection> {
  if (sharedClient?.isConnected) {
    return sharedClient;
  }

  if (sharedClient && !sharedClient.isConnected) {
    sharedClient.disconnect();
    sharedClient = null;
  }

  if (!connectPromise) {
    connectPromise = (async () => {
      if (!sharedClient || !sharedClient.isConnected) {
        if (sharedClient) {
          sharedClient.disconnect();
        }
        sharedClient = createClient();
        await connectClient(sharedClient);
        log.info("Persistent connection established", {
          host: CIVIL3D_HOST,
          port: CIVIL3D_PORT,
        });
      }
      return sharedClient;
    })().finally(() => {
      connectPromise = null;
    });
  }

  return connectPromise;
}

/** Reset the shared connection after a transport failure. */
export function resetSharedConnection(): void {
  if (sharedClient) {
    sharedClient.disconnect();
    sharedClient = null;
  }
}

/**
 * Runs an operation over the persistent Civil 3D connection.
 * Reconnects once on failure before surfacing the error.
 */
export async function withApplicationConnection<T>(
  operation: (client: ApplicationClientConnection) => Promise<T>
): Promise<T> {
  try {
    const client = await getSharedConnection();
    return await operation(client);
  } catch (error) {
    log.warn("Operation failed, retrying with fresh connection", {
      error: error instanceof Error ? error.message : String(error),
    });
    resetSharedConnection();
    const client = await getSharedConnection();
    return await operation(client);
  }
}
