# なでもふセットアップ(TailMoverSetup)

VRChatのアバターで使用できる，尻尾や腕の動きをアニメーションとして保存するツールです．

- VRChatのデスクトップモードでもExpressionMenuで腕を動かしたい．
- アバターの耳をExpressionMenuから自由に動かしたい．
- アバターの尻尾のアイドルモーションを設定したい．

という方のためのツールです．

## 導入方法
1. あらかじめアバターアップロード用プロジェクトのバックアップを取っておく．
2. VRCSDK3-AVATARを最新版に更新する．
3. AvatarPenSetupTool.unitypackageをUnityProjectにインポートする．
4. Unityの上部メニュー，Window/HhotateA/TailMoverSetupを開く．
5. "Avatar"の欄にシーン上のアバターオブジェクトをドラッグ&ドロップで参照する
6. 動かしたい部位のプリセットを選択する．
7. 動かしたい部位(尻尾)の根元のオブジェクトをroot bonesに設定する． 
8. 初期状態で伸ばす方向をTailAxiに設定する．
    正面方向に伸ばしたいなら(0,0,1)のようにする．
    (0,0,0)の場合はそのままの状態を初期状態にする.
9. ”Setup”ボタンを押す．
10. testRotXおよびtestRotYを動かして，動かしたい部位の動きを確認する．
11. 修正したい場合はRotSettiongsからUp,Down,Right,LeftそれぞれのAbgleを設定し直し，再度確認する．
12. ControllMode（メニューから自由に動かしたい）にしたい場合は"Save RadialControll"ボタンを押す．
    IdleMode(勝手に動くアニメーション)にしたい場合は"Save IdleMotion"ボタンを押す．
13. ウィンドウを閉じて，アバターをアップロードする．

## 使用方法
### ControllModeの場合
1. ExpressionMenuから○○○○Controllを選択
2. うごかせるよー
### IdleModeの場合
1. ExpressionMenuから○○○○Idleを選択
2. 動かすスピードを設定

## アンインストール手順
### v1.27以降
 1. 本ツールのVRChatNotRecommendedオプションから"Force Revert"ボタンを押す．
 2. 「Status : Complete Revert」というメッセージが出れば成功
### v1.26以前
1. Fx_Animatorから"TailMover_"から始まる名前のレイヤーを削除する．
2. VRCExpressionsMenuから"TailMover_"から始まる名前の項目を削除する．
3. VRCExpressionParameters"TailMover_"から始まる名前の項目を削除する．

## 注意事項
- アバターのfxAnimatorController,ExpressionMenu,ExpressionParametersに破壊的な変更を加えます．あらかじめ忘れずにバックアップを取ってください．
- ExpressionParameters,ExpressionMenuの項目が上限に達していた場合，正常に導入できない場合があります．その場合は一時的に項目を減らすなどの対処をお願い致します．
- 過去バージョンと競合してエラーが出る場合はFullPackageを試してください．

## 利用規約
- アバターへの同梱，改良，ツールの一部，まるごと含め，二次配布可とします．
- 二次配布する場合，連絡とクレジット表記があるとうれしいです．(必須ではありません)
- 本ツールを使用して発生した問題に対しては製作者は一切の責任を負いません.
- VRChatやUnity等の仕様変更により本ツールの機能が使えなくなった場合、製作者は責任を負いません。

## 動作確認環境
- Unity2019.4.24f1
- VRCSDK3-AVATAR-2021.08.11.15.16_Public

## 制作者
@HhotateA_xR
問題報告は https://github.com/HhotateA/AvatarModifyTools へ

## 更新履歴
2021/07/31 v1.25
2021/08/13 v1.26
2021/08/27 v1.27