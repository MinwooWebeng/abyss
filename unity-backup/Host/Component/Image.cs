using System.IO.MemoryMappedFiles;
using UnityEngine;

namespace AbyssEngine.Component
{
    internal class Image : IComponent
    {
        public Image(AbyssCLI.ABI.File arg)
        {
            _mmap_file = MemoryMappedFile.OpenExisting(
                arg.MmapName,
                MemoryMappedFileRights.Read
            );

            UnityTexture2D = new Texture2D(2, 2);

            var fileView = _mmap_file.CreateViewAccessor(arg.Off, arg.Len);
            var fileData = new byte[fileView.Capacity];
            fileView.ReadArray(0, fileData, 0, (int)fileView.Capacity);
            UnityTexture2D.LoadImage(fileData);
        }
        public void Dispose()
        {
            _mmap_file.Dispose();
        }

        public Texture2D UnityTexture2D { get; private set; }
        private readonly MemoryMappedFile _mmap_file;
    }
}
