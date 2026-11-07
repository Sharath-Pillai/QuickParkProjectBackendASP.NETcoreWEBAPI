namespace QuickParkAPI.DTOs
{
    public class QuickChatMessageDto
    {
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    public class QuickChatRequestDto
    {
        public string Message { get; set; } = string.Empty;
        public List<QuickChatMessageDto> History { get; set; } = new();
    }

    public class QuickChatResponseDto
    {
        public string Message { get; set; } = string.Empty;
        public List<string> Suggestions { get; set; } = new();
        public List<QuickChatArtifactDto> Artifacts { get; set; } = new();
    }

    public class QuickChatArtifactDto
    {
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Href { get; set; } = string.Empty;
    }
}
