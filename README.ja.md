# UnityNaturalMCP

[English](README.md)

UnityNaturalMCPは、"ナチュラル"な使い勝手を目指した、Unity向けのMCPサーバー実装です。

Unity C#で定義したMCPツールを、ダイレクトにClaudeCodeやGitHub Copilot + VSCodeなどのMCPクライアントから利用できます。

## Features
- Unity EditorとMCPクライアント間の簡潔な通信フロー
- stdio/Streamable HTTP対応
- [MCP C# SDK](https://github.com/modelcontextprotocol/csharp-sdk)を用いた、C#で完結する拡張MCPツールの実装
- ClaudeCode, GitHub Copilot + VSCodeサポート

## Requirements
- Unity 6000.0
- Node.js 18.0.0 or later

## Architecture
```mermaid
graph LR
A[MCP Client] ---|Streamable HTTP| C[UnityNaturalMCPServer]
```

または

```mermaid
graph LR
A[MCP Client] ---|stdio| B[stdio-to-streamable-http]
B ---|Streamable HTTP| C[UnityNaturalMCPServer]
```

### UnityNaturalMCPServer
Unity Packageとして提供される、 `Streamable HTTP` として振る舞うMCPサーバー実装です。

`Github Copilot + VSCode` などの `Streamable HTTP` 対応のクライアントであれば、これを介して単体でUnity Editorと通信することができます。

### stdio-to-streamable-http
Node.jsで実装された、MCPクライアントとUnity間の通信を中継する `stdio` ベースのMCPサーバーです。

`ClaudeCode` などの一部のMCPクライアントは、2025/6/15現在、 `Streamable HTTP` に対応していません。

`stdio` の入力を `Streamable HTTP` にバイパスすることで、 `UnityNaturalMCPServer` とMCPクライアントの間の通信を可能にします。

### UnityNaturalMCPTest
機能検証用、サンプルとなるUnityプロジェクトです。

## MCP Tools
現在、次のMCPツールが実装されています。

- **RefreshAssets**: Unity Editorのアセットを更新
- **GetLogHistory**: Unity Consoleのログ履歴を取得

## Installation

### Unity
動作には、次のPackageが必要です。
- [UniTask](https://github.com/Cysharp/UniTask)
- [NugetForUnity](https://github.com/GlitchEnzo/NuGetForUnity)

また、NugetForUnityより、次のNuget Packageをインストールしてください。
- [System.Text.Json](https://www.nuget.org/packages/System.Text.Json/)
- [ModelContextProtocol](https://www.nuget.org/packages/ModelContextProtocol/)
- [Microsoft.Extensions.DependencyInjection](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection/)

> [!WARNING]
> ModelContextProtocolはまだpreview段階です。NugetForUnityを介して導入する場合、`Show Prerelease`トグルを有効化する必要があります。

UPM(Unity Package Manager)を介してインストールできます。

- `Packages/manifest.json` を編集
- `dependencies` セクションに以下を追加：
  ```json
  "jp.notargs.unity-natural-mcp": "https://github.com/notargs/UnityNaturalMCP.git?path=/UnityNaturalMCPServer"
  ```

### Initial Setup
1. Unity Editorで`Edit > Preferences > Unity Natural MCP`を開く
2. MCPサーバーのポート番号を設定（デフォルト: 8090）
3. `Refresh` ボタンをクリックして設定を反映

![Preference](docs/images/preferences.png)

### Claude Code
RepositoryをCloneし、次のコマンドを利用して、ClaudeCodeにMCPサーバーを登録します。

`path/to` は、Cloneした `stdio-to-streamable-http` のパスに置き換えてください。
```shell
claude mcp add-json unity-natural-mcp -s project '{
  "type":"stdio",
  "command": "npm",
  "args": ["start", "--prefix", "path/to/stdio-to-streamable-http/"],
  "env": {
    "MCP_SERVER_IP": "localhost",
    "MCP_SERVER_PORT": "8090"
  }
}'
```

`MCP_SERVER_IP` `MCP_SERVER_PORT` 環境変数を介して、接続先のIPアドレスとポートを指定することができます。

### WSL2
Windows上でClaude Codeなどを用いてMCPを利用する場合、WSL2を利用する必要があります。

WSL2とUnityの連携を行うためには、WSL2とホストOSのネットワーク設定を適切に行う必要があります。

簡単なアプローチは、ミラーモードを使用して、WSL2とホストOSを接続する方法です。

ミラーモードを有効化するためには、`C:/Users/[username]/.wslconfig`へと以下の設定を追加します。
```ini
[wsl2]
networkingMode=mirrored
```

ミラーモードでは、localhostを介してWSL2とホストOSの間で通信することができます

しかしながら、C#サーバー側でlocalhostにバインドした場合、期待通りに動作せず、接続が失敗する場合があります。

これを回避するためには、Unityの`Edit > Preferences > Unity Natural MCP`より、IPAddressを`*`に設定し、`Refresh`を実行します。

> [!CAUTION]
> セキュリティ上の観点から、IP Addressに`*`を指定することは本来推奨されません。
> こちらはあくまで簡易的なセットアップ手順を示すものです。
> 環境に応じて、適宜調整を行ってください。

### VSCode + GitHub Copilot
VSCode + GitHub Copilotを利用する場合、Streamable HTTPを介した接続が可能です。

`.vscode/mcp.json` に次の設定を追加します。

```json
{
  "servers": {
    "unity-natural-mcp": {
      "url": "http://localhost:8090/mcp"
    }
  }
}
```

## Custom MCP Tool Implementation

### 1. Create MCP Tool
UnityNaturalMCPでは、[ModelProtocolContext C#SDK](https://github.com/modelcontextprotocol/csharp-sdk)を用いて、C#でMCPツールを実装することができます。

Editor用のasmdefを作成し、次のスクリプトファイルを追加します。

```csharp
using UnityEngine;
using UnityNaturalMCP.Editor.Attributes;
using System.ComponentModel;

[McpServerToolType, Description("カスタムMCPツールの説明")]
public class MyCustomMCPTool
{
    [McpServerTool, Description("メソッドの説明")]
    public string MyMethod()
    {
        return "Hello from Unity!";
    }
}
```

### 2. Create MCP Tool Builder
MCPツールをMCPサーバーに登録するためには、`McpBuilderScriptableObject`を継承したクラスを作成します。
```csharp
using UnityEngine;
using UnityNaturalMCP.Editor;

[CreateAssetMenu(fileName = "MyCustomMCPToolBuilder", 
                 menuName = "UnityNaturalMCP/My Custom Tool Builder")]
public class MyCustomMCPToolBuilder : McpBuilderScriptableObject
{
    public override void Build(IMcpServerBuilder builder)
    {
        builder.WithTools<MyCustomMCPTool>();
    }
}
```


### 3. Create ScriptableObject
1. Unity Editorでプロジェクトウィンドウを右クリック
2. `Create > UnityNaturalMCP > My Custom Tool Builder` を選択してScriptableObjectを作成
3. `Edit > Preferences > Unity Natural MCP > Refresh` から、MCPサーバーを再起動すると、作成したツールが読み込まれます。

### Best practices for Custom MCP Tools

#### MCPInspector
MCPInspectorから、Streamable HTTPを介してMCPツールを呼び出し、動作確認をスムーズに行うことができます。

![MCPInspector](docs/images/mcp_inspector.png)

#### Error Handling
MCPツール内でエラーが発生した場合、それはログに表示されません。

try-catchブロックを使用して、エラーをログに記録し、再スローすることを推奨します。

```csharp
[McpServerTool, Description("エラーを返す処理の例")]
public async void ErrorMethod()
{
  try
  {
      throw new Exception("This is an error example");
  }
  catch (Exception e)
  {
      Debug.LogError(e);
      throw;
  }
}
```

#### Asynchonous Processing
UnityのAPIを利用する際は、メインスレッド以外から呼び出される可能性を考慮する必要があります。

また、戻り値の型には、 `Task<T>` を利用する必要があります。

```csharp
[McpServerTool, Description("非同期処理の例")]
public async Task<string> AsyncMethod()
{
    await UniTask.SwitchToMainThread();
    await UniTask.Delay(1000);
    return "非同期処理完了";
}
```

## License

MIT License