# Claude Code project instructions

Use the browser-only product defined in docs 00–11. Any old native-app assumptions are invalid.

- User promise: open the same HTTPS website on phone and computer; no installation/account.
- Flow: desktop creates receiver room and QR; phone scans, chooses video, receiver consents.
- ASP.NET Core .NET 10 + SignalR cloud service hosts web/signaling. Video never goes through app server in MVP.
- WebRTC DataChannel is the only media path. No TURN, HTTP upload, cloud bucket or silent relay fallback. ICE failure must be clear.
- Frontend TypeScript; target first: Android Chrome sender → desktop Chrome/Edge receiver. iOS Safari sender is a device gate, not an assumed pass. Mobile receiving is out of MVP.
- 64 KiB chunks, bounded `bufferedAmount`, receiver ACK offset after write, verify final size/SHA-256. Never buffer entire video in RAM.
- No promise of background or post-kill resume. Same-page reconnect is best effort; if source handle is lost, ask user to reselect and verify identity.
- No database required in single-instance MVP. Unjoined QR invitation TTL is 10 minutes; active room has 30-minute idle timeout and 72-hour hard limit. Same-page reconnect uses peer-scoped rotating tokens; reload is not guaranteed. Do not persist/log SDP, ICE, room secrets, filenames, or file bytes.
- First implement browser compatibility/storage spikes in docs 07 before feature breadth. `showSaveFilePicker` is optional and must be feature-detected; never claim universal support.
- Read docs 00–09 in order. Implement work packages only after feasibility gates. Keep diagrams and all docs consistent with the chosen data path.
- Do not claim completion or large-file support from compile/unit tests alone; collect real-device transfer evidence per docs 09.

Repository convention: .NET 10/C# 13. Lock SDK/package versions after checking the actual deployment toolchain. The solution must build and run locally without requiring the end user to install anything; deployment operator may package/run the server using documented hosting approach.
