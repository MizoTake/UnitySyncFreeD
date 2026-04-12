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
- `Multicast TTL`
- `Join Multicast Group`
- `Multicast Interface Address`

sample ではネットワーク設定を `ScriptableObject` preset に分けて扱います。

- `FreeDUdpOutputProfileAsset`: Free-D 送信設定
- `FreeDUdpInputProfileAsset`: Free-D / loopback 受信設定

送信タイミングや pose 選択などの挙動設定は `SyncFreeDBehaviourProfileAsset` 側で切り替えます。

- `LateUpdate`
- `FixedInterval`
- `FixedUpdate`
- `ManualTick`

Inspector / Debug Window では次の運用指標を確認できます。

- `Configured Destinations`
- `Last Requested Destinations`
- `Effective Send Mode`
- `Send Success / Failure`
- `Destination spread (us)`
- `Destination send order / endpoint / success`
- `Skipped By Filter`
- `Multicast Configured`

Editor では multicast 用に次も補助します。

- 利用可能な IPv4 NIC 一覧
- Bind Address の即時選択
- Multicast Interface Address の即時選択
- 複数 NIC 時の Bind / Multicast Interface 明示 warning
- Bind / Multicast 設定の妥当性 warning
- Destination Port / Additional Destinations / Camera ID Filter の妥当性 warning
- Multicast TTL の妥当性 warning

Free-D の受信側では次を扱います。

- `FreeDInputSourceBehaviour`: UDP unicast / multicast 受信、D1 packet parse、camera id filter
- `FreeDInputSourceBehaviour`: 同一 UDP port を共有する複数 receiver への packet 多重配布
- `FreeDDrivenCameraBehaviour`: 受信した pose を Transform に、lens を Unity Camera の `focalLength` / `focusDistance` に反映
