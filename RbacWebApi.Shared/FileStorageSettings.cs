namespace RbacWebApi.DTOs;

/// <summary>
/// 文件存储配置（从 appsettings.json FileStorage 节点绑定）
/// </summary>
public class FileStorageSettings
{
    /// <summary>文件存储根目录（相对路径会以 Web 项目运行目录为基准）</summary>
    public string BasePath { get; set; } = "./files";

    /// <summary>分片临时存储目录（相对于 BasePath）</summary>
    public string ChunkDir { get; set; } = ".chunks";
}
