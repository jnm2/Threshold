using System;

namespace Threshold
{
    public readonly struct BackupItem
    {
        public string Description { get; }
        public BackupContentType ContentType { get; }
        public string FileName { get; }
        public ReadOnlyMemory<byte> Content { get; }

        public BackupItem(string description, BackupContentType contentType, string fileName, ReadOnlyMemory<byte> content)
        {
            Description = description;
            ContentType = contentType;
            FileName = fileName;
            Content = content;
        }
    }
}
