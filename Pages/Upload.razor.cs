using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;


namespace visual_binary_analysis.Pages
{
    public struct FileData(byte[] bytes)
    {
        public byte[] Bytes { get; } = bytes;
        public List<string> Hex { get; } = BytesToHex(bytes);
        public string Text { get; } = Encoding.UTF8.GetString(bytes);

        public ElementReference[] Refs {get; set;} = EmptyRefs(bytes);

        public List<string> HexHovers {get; set;} = Enumerable.Repeat(string.Empty, bytes.Length).ToList();
        public List<string> TextHovers {get; set;} = Enumerable.Repeat(string.Empty, bytes.Length).ToList();

        static private ElementReference[] EmptyRefs(byte[] bytes)
        {
            var refs = new ElementReference[bytes.Length];
            for (int i = 0; i < bytes.Length; i++)
            {
                refs[i] = new ElementReference();
            }
            return refs;
        } 

        static private List<string> BytesToHex(byte[] bytes)
        {
            List<string> _fileHex = new(bytes.Length);
            foreach (var b in bytes)
            {
                _fileHex.Add(b.ToString("X2"));
            }
            return _fileHex;
        }
    }

    public partial class Upload : IAsyncDisposable
    {
        ElementReference fileDropContainer;
        InputFile? inputFile;

        IJSObjectReference? _filePasteModule;
        IJSObjectReference? _filePasteFunctionReference;

        private string? HoverClass;

        private FileData fileData;
        private string? ErrorMessage;


        void OnDragEnter(DragEventArgs e) => HoverClass = "hover";

        void OnDragLeave(DragEventArgs e) => HoverClass = string.Empty;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                _filePasteModule = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./js/filePaste.js");

                _filePasteFunctionReference = await _filePasteModule.InvokeAsync<IJSObjectReference>("initializeFilePaste", fileDropContainer, inputFile?.Element);
            }
        }

        async Task OnChange(InputFileChangeEventArgs e)
        {
            ErrorMessage = string.Empty;

            var file = e.File;
            using var stream = file.OpenReadStream();
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            var fileBytes = ms.ToArray();
            fileData = new FileData(fileBytes);

            // source = $"data:{file.ContentType};base64,{Convert.ToBase64String(fileBytes)}";

            HoverClass = string.Empty;
        }

        static string GetOpacity(string hex)
        {
            return "var(--byte-" + hex[0].ToString() + ")";
        }

        private async Task OnHexHover(int index)
        {
            await JSRuntime.InvokeVoidAsync("addActive", "text-" + index.ToString());
            await JSRuntime.InvokeVoidAsync("addActive", "byte-" + index.ToString());
        }

        private async Task OnHexLeave(int index)
        {
            await JSRuntime.InvokeVoidAsync("removeActive", "text-" + index.ToString());
            await JSRuntime.InvokeVoidAsync("removeActive", "byte-" + index.ToString());
        }

        private async Task OnTextHover(int index)
        {
            await JSRuntime.InvokeVoidAsync("addActive", "hex-" + index.ToString());
            await JSRuntime.InvokeVoidAsync("addActive", "byte-" + index.ToString());
        }

        private async Task OnTextLeave(int index)
        {
            await JSRuntime.InvokeVoidAsync("removeActive", "hex-" + index.ToString());
            await JSRuntime.InvokeVoidAsync("removeActive", "byte-" + index.ToString());
        }

        public async ValueTask DisposeAsync()
        {
            if (_filePasteFunctionReference != null)
            {
                await _filePasteFunctionReference.InvokeVoidAsync("dispose");
                await _filePasteFunctionReference.DisposeAsync();
            }

            if (_filePasteModule != null)
            {
                await _filePasteModule.DisposeAsync();
            }
        }
    }
}