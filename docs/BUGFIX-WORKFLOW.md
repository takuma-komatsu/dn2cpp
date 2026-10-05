# バグ修正の継続ワークフロー

本書に従ったチケット消化の開始依頼を受けたときにループを開始する。
本書を読む・編集する・レビューする依頼だけでは開始しない。開始依頼は、
本書と repository 規約を満たす範囲で、調査・修正・回帰・文書更新・commit・
push・PR 作成・rebase merge まで進める依頼として扱う。

[STATUS.md](STATUS.md) の高優先度バグを減らす継続作業に適用する。
着手順と PR の粒度は担当者が判断する。小さい複数チケットを同じ PR に
まとめてよいが、その中でも順次実装・確認する。異なるチケットや PR の
実装は並行せず、現在の PR が実際に merged になるまで次の PR の対象へ
着手しない。

ビルド・gate・module 境界・コード規約は [AGENTS.md](../AGENTS.md)、
merge に必要な検証は [CONTRIBUTING.md](../CONTRIBUTING.md) に従う。
本書は継続ループの運用を定め、これらの細則を置き換えない。

開始時は作業ツリーの状態を確認し、既存の作業を保持して main を最新化する。
AGENTS.md、CONTRIBUTING.md、[README.md](../README.md)、
[ARCHITECTURE.md](ARCHITECTURE.md)、STATUS.md を実際に読む。Claude は
[CLAUDE.md](../CLAUDE.md) の `@AGENTS.md` も読む。対象を選定し、方針の提示だけで
止まらず修正作業へ進む。

## 担当と独立レビュー

親セッションは調査・調整・統合・検証を担当し、コード・テスト・文書の
実装は AGENTS.md の指示どおりサブエージェントへ委譲する。同じ変更の
実装担当とレビュアーへの委譲は可能だが、書込みの担当を明確にし、同じ
ファイルや artifact への競合を避ける。

実装完了後、独立した reviewer に実際にレビューを委譲する。運用上の
指定は **Codex / Astra / xhigh**、**Claude / Opus / xhigh** とする。
どちらも reasoning effort（思考強度）を xhigh に指定する。自己レビューや
別モデルのレビューで代用しない。指定モデルまたは思考強度を利用できない
場合、レビュー条件を達成済みとせず merge しない。

レビューは再現・根拠のある正しさ、デグレ、必要な回帰、repository invariant
に絞る。無関係な refactor、仕様拡大、些末な style 修正、収拾がつかなくなる
要求を追加しない。main 由来の既存未対応と PR が導入した不具合を区別する。
当該 PR の不具合は必ず直し、独立 reviewer の指摘がゼロになるまで修正・
再レビューを繰り返す。merge 前に実際の最終差分がレビュー対象であることを
確認する。

## 再現・修正・検証

近い既存 bucket に回帰を追加し、修正前の失敗と .NET の実際の挙動を先に
確認する。修正後は関連 build と適用される gate を実行し、CLR の実出力との
parity、新しい block が実行された証拠、旧全体出力が unchanged prefix として
残ることを確認する。テストや期待出力を実装に都合よく変えて通さない。
意図した既存 divergence の扱いも AGENTS.md に従う。

検証中は source や checkout を変更しない。build・gate・実行で書込みを行う
プロセス（writer）の終了を確認し、セッションを回収してから次の編集や
checkout、commit 整理へ進む。

main の潜在バグを見つけた場合、確実に直せる関連の些末なものは同時に修正
する。リスクが高いものは再現・原因・必要な対応を STATUS.md に新規起票し、
次の候補にする。STATUS.md は未完了の作業だけを保持し、修正対象行の削除を
PR 差分に含め、merge で取り除く。チケット ID は STATUS.md だけに置き、
コード・gate・文書・commit・PR など他の場所へ引用しない。

## commit と PR

ブランチで導入したバグやデグレの修正は amend し、必要なら interactive
rebase で既存の該当 commit に統合する。追加の fix commit を最終履歴に残さず、
commit は必要最小限の理想的な形に整理する。push 済みの amend は
force-with-lease を使う。PR 本文やコメントにも修正経緯を書かない。

PR の title は英語、本文は簡潔な日本語とする。本文には最終的な挙動と、
変更の評価に必要な回帰範囲・実質的な未検証事項だけを記載する。作業経緯、
amend 履歴、レビューや gate の実行状況、自明な検証対象外・skip 理由、
人間専用手順の未実施注記は書かない。

## merge と次の作業

必要なローカル検証、最終 head の必須 CI 成功、最終差分に対する独立 reviewer
の指摘ゼロを確認してから rebase merge する。hosted smoke CI だけを merge の
根拠にしない。skip や中断を pass と扱わない。実際に MERGED になったことを
確認し、main を更新してから次の対象へ進む。

通常操作ごとの確認を繰り返さず、ユーザーの中断・停止指示か修正可能な対象が
なくなるまで継続する。ひとつの PR の完了や待ち時間だけでループを終了しない。
必須条件を満たせず進めない場合は、不足している条件と現在の状態を正直に
報告し、pass や merge 済みとして扱わない。途中引継ぎファイルは作らず、
現在の PR・branch・HEAD・進捗を本書に固定しない。

開始時の指示例:

> docs/BUGFIX-WORKFLOW.mdに従ってチケット消化を開始してください。
