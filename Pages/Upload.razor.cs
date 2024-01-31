using System.Numerics;
using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;


namespace visual_binary_analysis.Pages
{
    public enum TextEncoding
    {
        UTF8,
        UTF7,
        UTF32,
        Unicode,
        ASCII
    }
    public struct FileData(byte[] bytes)
    {
        public int PageIndex { get; set; } = 0;
        static public int PageSize { get; set; } = 2048;
        public List<Page> Pages { get; set; } = BuildPages(bytes, PageSize);
        public static TextEncoding TextEncoding { get; set; } = TextEncoding.UTF8;

        public readonly struct Page(byte[] bytes)
        {
            public byte[] Bytes { get; } = bytes;
            public List<string> Hex { get; } = BytesToHex(bytes);
            public readonly string Text()
            {
                return TextEncoding switch
                {
                    TextEncoding.UTF8 => Encoding.UTF8.GetString(Bytes),
                    TextEncoding.Unicode => Encoding.Unicode.GetString(Bytes),
                    TextEncoding.ASCII => Encoding.ASCII.GetString(Bytes),
                    TextEncoding.UTF32 => Encoding.UTF32.GetString(Bytes),
                    _ => Encoding.UTF8.GetString(Bytes),
                };
            }
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

        public static void SetTextEncoding(TextEncoding encoding)
        {
            TextEncoding = encoding;
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

            IBrowserFile file = e.File;
            long maxFileSize = 1024L * 1024L * 1024L * 2L;
            using var stream = file.OpenReadStream(maxAllowedSize: maxFileSize);
            using MemoryStream ms = new();
            await stream.CopyToAsync(ms);
            byte[] fileBytes = ms.ToArray();
            fileData = new FileData(fileBytes);

            // source = $"data:{file.ContentType};base64,{Convert.ToBase64String(fileBytes)}";

            HoverClass = string.Empty;
        }

        private async Task OnHover(string source, int index)
        {
            if (index > FileData.PageSize)
                return;
            switch (source)
            {
                case "hex":
                    await JSRuntime.InvokeVoidAsync("addClass", "text-" + index.ToString(), "active");
                    await JSRuntime.InvokeVoidAsync("addClass", "byte-" + index.ToString(), "active");
                    break;
                case "text":
                    await JSRuntime.InvokeVoidAsync("addClass", "hex-" + index.ToString(), "active");
                    await JSRuntime.InvokeVoidAsync("addClass", "byte-" + index.ToString(), "active");
                    break;
                case "byte":
                    await JSRuntime.InvokeVoidAsync("addClass", "hex-" + index.ToString(), "active");
                    await JSRuntime.InvokeVoidAsync("addClass", "text-" + index.ToString(), "active");
                    break;
            }
        }

        private async Task OnLeave(string source, int index)
        {
            if (index > FileData.PageSize)
                return;
            switch (source)
            {
                case "hex":
                    await JSRuntime.InvokeVoidAsync("removeClass", "text-" + index.ToString(), "active");
                    await JSRuntime.InvokeVoidAsync("removeClass", "byte-" + index.ToString(), "active");
                    break;
                case "text":
                    await JSRuntime.InvokeVoidAsync("removeClass", "hex-" + index.ToString(), "active");
                    await JSRuntime.InvokeVoidAsync("removeClass", "byte-" + index.ToString(), "active");
                    break;
                case "byte":
                    await JSRuntime.InvokeVoidAsync("removeClass", "hex-" + index.ToString(), "active");
                    await JSRuntime.InvokeVoidAsync("removeClass", "text-" + index.ToString(), "active");
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
        public void SetTextEncoding(TextEncoding encoding)
        {
            FileData.SetTextEncoding(encoding);
            StateHasChanged();
        }

        // public void Search()
        // {
        //     Console.WriteLine("hi");
        //     Console.WriteLine(SearchText);
        // }
        // private async Task Search(Microsoft.AspNetCore.Components.ChangeEventArgs patharg)
        // {
        //     var path = (string)patharg.Value;
        //     Console.WriteLine(path);
        // }

        private readonly List<int> HighlightedChars = [];

        private async Task ClearHighlight()
        {
            if (HighlightedChars.Count == 0)
                return;
            for (int i = 0; i < HighlightedChars.Count; i++)
            {
                int index = HighlightedChars[i];
                await JSRuntime.InvokeVoidAsync("removeHighlight", "text-" + index.ToString());
            }
            HighlightedChars.Clear();
        }

        private async Task HighlightRange(int start, int length)
        {
            if (length == 0)
                return;

            for (int i = start; i < start + length; i++)
            {
                if (i == start || i == 0)
                    await JSRuntime.InvokeVoidAsync("addClass", "text-" + i.ToString(), "highlight-start");
                if (i == start + length - 1)
                    await JSRuntime.InvokeVoidAsync("addClass", "text-" + i.ToString(), "highlight-end");
                if (i != start && i != start + length - 1)
                    await JSRuntime.InvokeVoidAsync("addClass", "text-" + i.ToString(), "highlight");
                HighlightedChars.Add(i);
            }
        }

        private async Task OnChange(ChangeEventArgs e)
        {
            await ClearHighlight();
            if (e.Value == null)
                return;
            string? searchText = e.Value.ToString();
            if (searchText == null)
                return;

            for (int i = 0; i < fileData.Pages.Count; i++)
            {
                FileData.Page page = fileData.Pages[i];
                string text = page.Text();
                int index = text.IndexOf(searchText);
                if (index == -1)
                    continue;

                if (i != fileData.PageIndex)
                {
                    fileData.SetPage(i);
                    StateHasChanged();
                }
                await HighlightRange(index, searchText.Length);
                return;
            }
        }
    }
}