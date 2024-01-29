using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;


namespace visual_binary_analysis.Pages
{
    public struct FileData(byte[] bytes)
    {
        public int PageIndex { get; set; } = 0;
        static public int PageSize { get; set; } = 2048;
        public List<Page> Pages { get; set; } = BuildPages(bytes, PageSize);

        public readonly struct Page(byte[] bytes)
        {
            public byte[] Bytes { get; } = bytes;
            public List<string> Hex { get; } = BytesToHex(bytes);
            public string Text { get; } = Encoding.UTF8.GetString(bytes);
        }

        static private List<Page> BuildPages(byte[] bytes, int pageSize)
        {
            List<Page> pages = [];
            for (int i = 0; i < bytes.Length; i += pageSize)
            {
                Page page = new(bytes[i..Math.Min(bytes.Length - 1, i + pageSize)]);
                pages.Add(page);
            }
            return pages;
        }

        public readonly Page CurrentPage()
        {
            if (Pages.Count == 0)
            {
                return new Page([]);
            }
            return Pages[PageIndex];
        }

        public void SetPage(int pageNumber)
        {
            PageIndex = pageNumber;
        }

        public readonly int ByteStartIndex()
        {
            return PageIndex * PageSize;
        }

        static private List<string> BytesToHex(byte[] bytes)
        {
            return bytes.Select(b => b.ToString("X2")).ToList();
        }
    }

    public class Utils
    {
        public static string GetOpacity(string hex)
        {
            return "var(--byte-" + hex[0].ToString() + ")";
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

        private async Task OnHover(string source, int index)
        {
            if (index > FileData.PageSize) {
                return;
            }
            switch (source)
            {
                case "hex":
                    await JSRuntime.InvokeVoidAsync("addActive", "text-" + index.ToString());
                    await JSRuntime.InvokeVoidAsync("addActive", "byte-" + index.ToString());
                    break;
                case "text":
                    await JSRuntime.InvokeVoidAsync("addActive", "hex-" + index.ToString());
                    await JSRuntime.InvokeVoidAsync("addActive", "byte-" + index.ToString());
                    break;
                case "byte":
                    await JSRuntime.InvokeVoidAsync("addActive", "hex-" + index.ToString());
                    await JSRuntime.InvokeVoidAsync("addActive", "text-" + index.ToString());
                    break;
            }
        }

        private async Task OnLeave(string source, int index)
        {
            if (index > FileData.PageSize) {
                return;
            }
            switch (source)
            {
                case "hex":
                    await JSRuntime.InvokeVoidAsync("removeActive", "text-" + index.ToString());
                    await JSRuntime.InvokeVoidAsync("removeActive", "byte-" + index.ToString());
                    break;
                case "text":
                    await JSRuntime.InvokeVoidAsync("removeActive", "hex-" + index.ToString());
                    await JSRuntime.InvokeVoidAsync("removeActive", "byte-" + index.ToString());
                    break;
                case "byte":
                    await JSRuntime.InvokeVoidAsync("removeActive", "hex-" + index.ToString());
                    await JSRuntime.InvokeVoidAsync("removeActive", "text-" + index.ToString());
                    break;
            }
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

        public void SetPage(int pageNumber)
        {
            fileData.SetPage(pageNumber);
            StateHasChanged();
        }

    }
}