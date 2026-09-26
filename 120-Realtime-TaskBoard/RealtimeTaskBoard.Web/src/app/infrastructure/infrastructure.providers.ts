import { IBoardApiPort } from '../domain/ports/board-api.port';
import { ISignalRPort } from '../domain/ports/signalr.port';
import { BoardApiAdapter } from './http/board-api.adapter';
import { SignalRAdapter } from './realtime/signalr.adapter';

/**
 * Dependency Injection (DI): Angular DI container sẽ tự động inject BoardApiAdapter mỗi khi có class nào yêu cầu IBoardApiPort
 * Đây là điểm duy nhất kết nối Domain (abstract) với Infrastructure (concrete)
 * Khi muốn test, chỉ cần thay useClass thành MockBoardApiAdapter
 */
export const INFRASTRUCTURE_PROVIDERS = [
  { provide: IBoardApiPort, useClass: BoardApiAdapter },
  { provide: ISignalRPort, useClass: SignalRAdapter }
];
