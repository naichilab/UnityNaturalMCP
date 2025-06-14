using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace UnityFluxMCP.Editor.Managers
{
    /// <summary>
    /// Unity プロジェクトのコンパイルを管理するシンプルなマネージャー
    /// Domain Reloadingに対する複雑な永続化処理を削除し、シンプルで堅牢な実装にしています
    /// </summary>
    public class CompilationManager
    {
        // タイムアウト定数
        private const int CompilationStartTimeoutMs = 5000; // 5秒
        private const int CompilationTimeoutMs = 120000; // 120秒
        private const int FrameDurationMs = 16; // 約60FPS
        private const int ProgressReportIntervalMs = 5000; // 5秒
        private const int MaxErrorsToReturn = 10;
        private const int MaxWarningsToReturn = 5;
        private class CompilationMessage
        {
            public string message { get; set; }
            public string file { get; set; }
            public int line { get; set; }
            public int column { get; set; }
            public string type { get; set; }
        }
        
        /// <summary>
        /// コンパイル開始を待機します
        /// </summary>
        private async UniTask<bool> WaitForCompilationStart()
        {
            var elapsed = 0;
            
            while (!EditorApplication.isCompiling && elapsed < CompilationStartTimeoutMs)
            {
                await UniTask.Yield(); // 次のフレームまで待機
                elapsed += FrameDurationMs;
            }
            
            return EditorApplication.isCompiling;
        }
        
        /// <summary>
        /// コンパイル完了を待機します
        /// </summary>
        /// <returns>経過時間（ミリ秒）</returns>
        private async UniTask<int> WaitForCompilationCompletion()
        {
            var elapsed = 0;
            var lastProgressReport = 0;
            
            while (EditorApplication.isCompiling && elapsed < CompilationTimeoutMs)
            {
                await UniTask.Yield();
                elapsed += FrameDurationMs;
                
                // 進捗レポート（現在は何もしない）
                if (elapsed - lastProgressReport >= ProgressReportIntervalMs)
                {
                    lastProgressReport = elapsed;
                }
            }
            
            return elapsed;
        }
        
        /// <summary>
        /// タイムアウト結果を作成します
        /// </summary>
        private object CreateTimeoutResult(string compilationId, DateTime startTime)
        {
            Debug.LogError($"[CompilationManager] Compilation timeout after {CompilationTimeoutMs / 1000} seconds");
            return new
            {
                success = false,
                error = $"Compilation timeout after {CompilationTimeoutMs / 1000} seconds",
                compilationId,
                duration = (DateTime.Now - startTime).TotalSeconds,
                stillCompiling = EditorApplication.isCompiling
            };
        }

        /// <summary>
        /// プロジェクトのコンパイルを実行します
        /// Domain Reloadingが発生してもシンプルに処理を完了させます
        /// </summary>
        public async UniTask<object> CompileProjectAsync()
        {
            var compilationId = Guid.NewGuid().ToString();
            var startTime = DateTime.Now;
            
            try
            {
                // 初期状態でのコンパイルエラーをチェック
                var initialCompilationFailed = EditorUtility.scriptCompilationFailed;
                
                // コンパイルを開始
                if (!EditorApplication.isCompiling)
                {
                    CompilationPipeline.RequestScriptCompilation();
                }
                
                // コンパイル開始を待つ
                await WaitForCompilationStart();
                
                if (!EditorApplication.isCompiling)
                {
                    // コンパイルが必要ない場合も正常終了とする
                    return CreateCompilationResult(
                        compilationId,
                        startTime,
                        DateTime.Now - startTime,
                        initialCompilationFailed,
                        false
                    );
                }
                
                // コンパイル完了まで待機
                var compilationElapsed = await WaitForCompilationCompletion();
                
                if (compilationElapsed >= CompilationTimeoutMs)
                {
                    return CreateTimeoutResult(compilationId, startTime);
                }
                
                // コンパイル後の状態を確認
                var scriptCompilationFailed = EditorUtility.scriptCompilationFailed;
                
                // Domain Reloadingが必要な場合でも、現在の状態で結果を返す
                // Domain Reloading後の処理は呼び出し側に任せる
                var endTime = DateTime.Now;
                var duration = endTime - startTime;
                
                return CreateCompilationResult(
                    compilationId, 
                    startTime, 
                    duration, 
                    scriptCompilationFailed,
                    !scriptCompilationFailed && !initialCompilationFailed
                );
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CompilationManager] Compilation failed with exception: {ex.Message}\n{ex.StackTrace}");
                return new
                {
                    success = false,
                    error = ex.Message,
                    compilationId,
                    stackTrace = ex.StackTrace
                };
            }
        }
        
        /// <summary>
        /// コンパイル結果を作成します
        /// </summary>
        private static object CreateCompilationResult(
            string compilationId, 
            DateTime startTime, 
            TimeSpan duration, 
            bool hasErrors,
            bool domainReloadExpected)
        {
            var errors = new List<CompilationMessage>();
            var warnings = new List<CompilationMessage>();
            
            if (hasErrors)
            {
                AddCompilationError(errors);
            }
            
            var assemblyInfo = CheckAssemblies(warnings, domainReloadExpected);
            
            return BuildCompilationResultObject(
                compilationId,
                duration,
                hasErrors,
                domainReloadExpected,
                errors,
                warnings,
                assemblyInfo
            );
        }
        
        /// <summary>
        /// コンパイルエラーを追加します
        /// </summary>
        private static void AddCompilationError(List<CompilationMessage> errors)
        {
            errors.Add(new CompilationMessage
            {
                message = "Script compilation failed. Check Unity Console for detailed error messages.",
                type = "Error",
                file = "Unknown",
                line = 0,
                column = 0
            });
        }
        
        /// <summary>
        /// アセンブリの状態をチェックします
        /// </summary>
        private static AssemblyCheckResult CheckAssemblies(
            List<CompilationMessage> warnings, 
            bool domainReloadExpected)
        {
            var assemblies = CompilationPipeline.GetAssemblies();
            var totalAssemblies = 0;
            var missingAssemblies = 0;
            
            foreach (var assembly in assemblies)
            {
                totalAssemblies++;
                
                if (!File.Exists(assembly.outputPath) && !domainReloadExpected)
                {
                    missingAssemblies++;
                    warnings.Add(new CompilationMessage
                    {
                        message = $"Assembly {assembly.name} output file not found at {assembly.outputPath}",
                        type = "Warning",
                        file = assembly.name,
                        line = 0,
                        column = 0
                    });
                }
            }
            
            return new AssemblyCheckResult
            {
                TotalAssemblies = totalAssemblies,
                MissingAssemblies = missingAssemblies
            };
        }
        
        /// <summary>
        /// コンパイル結果オブジェクトを構築します
        /// </summary>
        private static object BuildCompilationResultObject(
            string compilationId,
            TimeSpan duration,
            bool hasErrors,
            bool domainReloadExpected,
            List<CompilationMessage> errors,
            List<CompilationMessage> warnings,
            AssemblyCheckResult assemblyInfo)
        {
            var message = GetCompilationMessage(hasErrors, domainReloadExpected);
            
            return new
            {
                success = !hasErrors,
                message,
                duration = duration.TotalSeconds,
                compilationId,
                errorCount = errors.Count,
                warningCount = warnings.Count,
                domainReloadExpected,
                assemblyInfo = new
                {
                    totalAssemblies = assemblyInfo.TotalAssemblies,
                    missingAssemblies = assemblyInfo.MissingAssemblies,
                    scriptCompilationFailed = hasErrors,
                    stillCompiling = EditorApplication.isCompiling
                },
                errors = errors.Select(e => new 
                {
                    e.message,
                    e.file,
                    e.line,
                    e.column
                }).Take(MaxErrorsToReturn).ToList(),
                warnings = warnings.Select(w => new
                {
                    w.message,
                    w.file,
                    w.line,
                    w.column
                }).Take(MaxWarningsToReturn).ToList()
            };
        }
        
        /// <summary>
        /// コンパイル完了メッセージを取得します
        /// </summary>
        private static string GetCompilationMessage(bool hasErrors, bool domainReloadExpected)
        {
            if (hasErrors)
                return "Compilation completed with errors";
            
            return domainReloadExpected 
                ? "Compilation completed successfully (Domain Reload expected)" 
                : "Compilation completed successfully";
        }
        
        private struct AssemblyCheckResult
        {
            public int TotalAssemblies { get; set; }
            public int MissingAssemblies { get; set; }
        }
    }
}