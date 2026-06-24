export interface Civil3dRpcErrorData {
  diagnostics?: Array<{
    id: string;
    severity: string;
    message: string;
    line: number;
    column: number;
  }>;
  pattern?: string;
  reason?: string;
  type?: string;
  stackTrace?: string;
  [key: string]: unknown;
}

export class Civil3dRpcError extends Error {
  readonly code: string;
  readonly data?: Civil3dRpcErrorData;

  constructor(code: string, message: string, data?: Civil3dRpcErrorData) {
    super(message);
    this.name = "Civil3dRpcError";
    this.code = code;
    this.data = data;
  }
}

export function formatCivil3dError(error: unknown): string {
  if (error instanceof Civil3dRpcError) {
    let text = `[${error.code}] ${error.message}`;

    if (error.data?.diagnostics?.length) {
      text += "\n\nCompilation diagnostics:";
      for (const d of error.data.diagnostics) {
        text += `\n  Line ${d.line}, Col ${d.column} (${d.severity}): ${d.message}`;
      }
    } else if (error.data) {
      text += `\n\nDetails:\n${JSON.stringify(error.data, null, 2)}`;
    }

    return text;
  }

  if (error instanceof Error) {
    return error.message;
  }

  return String(error);
}
