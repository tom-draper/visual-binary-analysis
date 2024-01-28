using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;


namespace visual_binary_analysis.Pages
{
    public partial class Upload : IAsyncDisposable
    {
        ElementReference fileDropContainer;
        InputFile? inputFile;

        IJSObjectReference? _filePasteModule;
        IJSObjectReference? _filePasteFunctionReference;

        private string? HoverClass;
        private byte[]? fileBytes;
        private List<string>? fileHex;
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
            Console.WriteLine(file.ContentType);
            using var stream = file.OpenReadStream();
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            fileBytes = ms.ToArray();
            fileHex = BytesToHex(fileBytes);

            // source = $"data:{file.ContentType};base64,{Convert.ToBase64String(fileBytes)}";

            HoverClass = string.Empty;
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

        string GetOpacity(string hex)
        {
            return "var(--byte-" + hex[0].ToString() + ")";
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