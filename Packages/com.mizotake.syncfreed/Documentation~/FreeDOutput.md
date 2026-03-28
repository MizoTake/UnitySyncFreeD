# FreeDOutput

初版では Free-D D1, 29 byte packet を生成します。

- checksum: `FreeDChecksumCalculator`
- encoding: `FreeDEncoding`
- packet build: `FreeDPacketBuilder`
- runtime transport: `FreeDUdpOutputBehaviour`

`FreeDUdpOutputBehaviour` は `PacketSendMode` に応じて以下を切り替えます。

- `SingleDestinationUnicast`
- `MultiDestinationUnicast`
- `Multicast`

また、Runtime では次の最小ネットワーク設定を持ちます。

- `Bind Address`
- `Socket Buffer Size`
- `Camera ID Filter`
- `Join Multicast Group`
- `Multicast Interface Address`

送信タイミングは `SyncFreeDBehaviour` 側で切り替えます。

- `LateUpdate`
- `FixedInterval`
- `FixedUpdate`
- `ManualTick`

Inspector / Debug Window では次の運用指標を確認できます。

- `Configured Destinations`
- `Last Requested Destinations`
- `Send Success / Failure`
- `Skipped By Filter`
- `Multicast Configured`

Editor では multicast 用に次も補助します。

- 利用可能な IPv4 NIC 一覧
- Multicast Interface Address の即時選択
- Bind / Multicast 設定の妥当性 warning
