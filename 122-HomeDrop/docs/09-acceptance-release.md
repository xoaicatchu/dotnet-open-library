# 09 — Nghiệm thu và phát hành

## Release gates

| Gate | Bằng chứng bắt buộc |
|---|---|
| G01 Product path | Android/iPhone browser → desktop; user completes open, scan, select, consent, save without installing or account |
| G02 Signaling-only server | Network capture confirms no video/file payload to HomeDrop origin; no file upload/download endpoints |
| G03 P2P | WebRTC selected candidate direct (host/srflx); TURN absent; ICE failure gives clear error |
| G04 Integrity | Receiver saved file size + SHA-256 match source fixture across small and claimed max sizes |
| G05 Memory | Browser process memory measured at increasing file sizes; no whole-file Blob/arrayBuffer; bounded queue |
| G06 Interruption | Wi-Fi toggle, tab background, peer close, reload; no false completion; documented resume limits |
| G07 Security | Room secret randomness/expiry/replay, origin allowlist, rate limits, oversized/malformed frame tests, privacy log scan |
| G08 Device support | Exact browser/OS versions and limitations published; no simulator-only claim |
| G09 Operations | HTTPS deployment, health/alerts, abuse controls, rollback, room cleanup verified |

## Test set

- Fixtures: 0 B, 1 B, 64 KiB−1, 64 KiB, 64 KiB+1, 100 MiB, 1 GiB, 5 GiB (only test sizes claimed by product).
- Deterministic bytes and expected SHA-256; never commit personal videos.
- Network cases: same Wi-Fi, guest network, cellular, VPN, internet disconnected, STUN blocked, restrictive NAT; drop ACK for last committed chunk and reconnect to prove receiver offset reconciliation and final SHA-256 still match.
- Device cases: Android Chrome phone + desktop Chrome/Edge; iPhone Safari + desktop Chrome/Edge; rotate/sleep/background/browser memory pressure.
- Security: expired/reused room, third peer join, origin spoof, signaling spam, message >limit, invalid offset/length, malicious filename, CSP.

## Example acceptance scenarios

```gherkin
Scenario: Direct successful transfer
  Given a receiver browser created a valid room and displays its QR
  And a sender browser joined and selected a fixture video
  When the receiver accepts and direct WebRTC connectivity succeeds
  Then the video payload travels only over the DataChannel between peers
  And the receiver saves a file whose size and SHA-256 match the fixture
  And the signaling service has no file bytes or filename in logs

Scenario: Direct connectivity fails
  Given two browsers joined the same room
  When ICE cannot establish a direct candidate pair within the timeout
  Then the transfer stops with a human-readable network message
  And no payload is uploaded to HomeDrop or relayed through TURN
```

## Release policy

No public claim “supports large videos” until G04/G05/G06 pass at a stated max size for each browser/platform. If iOS Safari or a browser storage strategy fails, list it as unsupported; do not silently fall back to cloud upload. Monitor connection success and failures only with privacy-safe aggregate telemetry. Have support flow explain VPN/guest Wi-Fi/strict NAT and offer retry; adding TURN requires fresh cost/privacy review and user-facing disclosure.
