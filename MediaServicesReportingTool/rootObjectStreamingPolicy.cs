
public class EnabledProtocols
{
    public bool download { get; set; }
    public bool dash { get; set; }
    public bool hls { get; set; }
    public bool smoothStreaming { get; set; }
}

public class NoEncryption
{
    public EnabledProtocols enabledProtocols { get; set; }
}

public class Properties
{
    public DateTime created { get; set; }
    public NoEncryption noEncryption { get; set; }
}

public class RootStreamingPolicy
{
    public string name { get; set; }
    public string id { get; set; }
    public string type { get; set; }
    public Properties properties { get; set; }
}