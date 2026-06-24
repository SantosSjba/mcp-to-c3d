import * as net from "net";
import { Civil3dRpcError, type Civil3dRpcErrorData } from "./civil3dError.js";
import { createLogger } from "./logger.js";
import { getCommandTimeoutMs } from "./timeouts.js";

const log = createLogger("SocketClient");

export interface SendCommandOptions {
  timeoutMs?: number;
}

export class ApplicationClientConnection {
  host: string;
  port: number;
  socket: net.Socket;
  isConnected: boolean = false;
  responseCallbacks: Map<string, (response: string) => void> = new Map();
  buffer: string = "";

  constructor(host: string, port: number) {
    this.host = host;
    this.port = port;
    this.socket = new net.Socket();
    this.setupSocketListeners();
  }

  private setupSocketListeners(): void {
    this.socket.on("connect", () => {
      this.isConnected = true;
      log.info("Connected", { host: this.host, port: this.port });
    });

    this.socket.on("data", (data) => {
      this.buffer += data.toString();
      this.processBuffer();
    });

    this.socket.on("close", () => {
      this.isConnected = false;
      log.debug("Connection closed");
    });

    this.socket.on("error", (error) => {
      log.error("Connection error", { error: String(error) });
      this.isConnected = false;
    });
  }

  /**
   * Attempt to parse the buffer as a complete JSON object.
   * If parsing fails, the data is incomplete — wait for more.
   */
  private processBuffer(): void {
    try {
      JSON.parse(this.buffer);
      // If parsing succeeds, we have a complete JSON response
      this.handleResponse(this.buffer);
      this.buffer = "";
    } catch {
      // Incomplete JSON — wait for more data
    }
  }

  public connect(): boolean {
    if (this.isConnected) {
      return true;
    }

    try {
      this.socket.connect(this.port, this.host);
      return true;
    } catch (error) {
      log.error("Failed to connect", { host: this.host, port: this.port, error: String(error) });
      return false;
    }
  }

  public disconnect(): void {
    this.socket.end();
    this.isConnected = false;
  }

  private generateRequestId(): string {
    return Date.now().toString() + Math.random().toString().substring(2, 8);
  }

  private handleResponse(responseData: string): void {
    try {
      const response = JSON.parse(responseData);
      const requestId = response.id || "default";

      const callback = this.responseCallbacks.get(requestId);
      if (callback) {
        callback(responseData);
        this.responseCallbacks.delete(requestId);
      }
    } catch (error) {
      log.error("Error parsing response", { error: String(error) });
    }
  }

  /**
   * Send a JSON-RPC command to the Civil 3D plugin and wait for a response.
   */
  public sendCommand(
    command: string,
    params: Record<string, unknown> = {},
    options?: SendCommandOptions
  ): Promise<unknown> {
    const timeoutMs = getCommandTimeoutMs(command, options?.timeoutMs);

    return new Promise((resolve, reject) => {
      try {
        if (!this.isConnected) {
          this.connect();
        }

        const requestId = this.generateRequestId();

        const commandObj = {
          jsonrpc: "2.0",
          method: command,
          params: params,
          id: requestId,
        };

        this.responseCallbacks.set(requestId, (responseData) => {
          try {
            const response = JSON.parse(responseData);
            if (response.error) {
              const err = response.error;
              reject(
                new Civil3dRpcError(
                  err.code ?? "CIVIL3D.UNKNOWN",
                  err.message ?? "Unknown error from Civil 3D plugin",
                  err.data as Civil3dRpcErrorData | undefined
                )
              );
            } else {
              resolve(response.result);
            }
          } catch (error) {
            if (error instanceof Error) {
              reject(new Error(`Failed to parse response: ${error.message}`));
            } else {
              reject(new Error(`Failed to parse response: ${String(error)}`));
            }
          }
        });

        const commandString = JSON.stringify(commandObj);
        log.debug("Sending command", { method: command, requestId });
        this.socket.write(commandString);

        setTimeout(() => {
          if (this.responseCallbacks.has(requestId)) {
            this.responseCallbacks.delete(requestId);
            log.warn("Command timed out", {
              method: command,
              requestId,
              timeoutMs,
            });
            reject(new Error(`Command timed out after ${timeoutMs}ms: ${command}`));
          }
        }, timeoutMs);
      } catch (error) {
        reject(error);
      }
    });
  }
}
