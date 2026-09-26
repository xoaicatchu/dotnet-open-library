export interface CreateColumnRequest {
  name: string;
  color?: string;
}

export interface UpdateColumnRequest {
  name: string;
  color?: string;
}

export interface ReorderColumnsRequest {
  columnIds: string[];
}
