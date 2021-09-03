# エモートモーションキット(EmoteMotionKit)

VRChatアバターのアイドルアニメーションや，エモートを設定できるツールです．
横になる睡眠モーションや，座りモーションをメニューから切り替えたり，
好きなアクションエモートを好きな時に再生することができます．

## 導入手順
1. あらかじめアバターアップロード用プロジェクトのバックアップを取っておく．
2. VRCSDK3-AVATARを最新版に更新する．
3. EmoteMotionKit.unitypackageをUnityProjectにインポートする．
4. Unityの上部メニュー，Window/HhotateA/EmoteMotionKitを開く.
5. "Avatar"の欄にシーン上のアバターオブジェクトをドラッグ&ドロップで参照する
6. レイヤーの設定をする．
    - EmoteLayer : EmoteMotionを追加するレイヤー
        - Base : Locomotionレイヤーに登録する．
        - Action : Actionレイヤーに登録する．（VRChatのエモートと同じ設定）
        - Additive : Idleレイヤーに登録する．（アイドルに加算されるアニメーション）
    - Use FX : FXレイヤーにアニメーションをコピーする．（表情などの合成用）
    - Is Saved : エモートの状態を保存する
7. アニメーションを登録する
8. アニメーションの設定を行う
    - TrackingSpace : （Animator Tracking Controlの設定）
        - TrackingBase : Trackingを優先する
        - FootAnimation : 足の動きをアニメーションで上書きする
        - BodyAnimation : 頭以外の動きをアニメーションで上書きする
        - AnimationBase : アニメーションを優先する
        - Emote : アニメーションで上書きする
    - IsEmote : エモート(ループしないアニメーション)として設定する．
    - Stop Locomotion : アニメーション以外での移動を禁止する（Animator Locomotion Controlの設定）
    - Enter Pose Space : 視点をアニメーションで移動する(Animator Temporary Pose Spaceの設定)
9. ”Setup”ボタンを押す．
10. 通常の手順でアバターをアップロードする．

## 使用方法
1. AvatarのExpressionMenuからEmoteMotionを選択する．
2. 任意のEmote or IdleAnimationを選択すると再生される．

## アンインストール手順
### v1.27以降
 1. 本ツールの"Modify Options"オプションから"Force Revert"ボタンを押す．
 2. 「Status : Complete Revert」というメッセージが出れば成功

## Modify Options
- Override Write Default : WriteDefaultの値を上書きします．(VRChat非推奨項目)
- RenameParameters : パラメーター名に含まれる2バイト文字をハッシュ化して取り除きます．
- Auto Next Page : メニューの項目数が上限に達した場合，自動で次ページを作成します．

- Force Revert : このツールでセットアップされた設定を元に戻します．

## 注意事項
- アバターのfxAnimatorController,ExpressionMenu,ExpressionParametersに破壊的な変更を加えます．あらかじめ忘れずにバックアップを取ってください．
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
2021/08/27 v1.27β
2021/09/03 v1.29