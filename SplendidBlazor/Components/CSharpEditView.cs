using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace SplendidBlazor.Components
{
    public class CSharpEditView : ComponentBase
    {
        [Inject] 
        public IJSRuntime _jsRuntime { get; set; }
        
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            var sequence = 0;
            
            builder.OpenElement(sequence++, "table");
            builder.AddAttribute(sequence++, "id", "tblMain");

            builder.OpenElement(sequence++, "tr");

            builder.OpenElement(sequence++, "td");
            builder.OpenElement(sequence++, "label");
            builder.AddContent(sequence++, "Name");
            builder.CloseElement();
            builder.OpenElement(sequence++, "input");
            builder.AddAttribute(sequence++, "type", "text");
            builder.CloseElement();
            builder.CloseElement();

            builder.OpenElement(sequence++, "td");
            builder.OpenElement(sequence++, "button");
            builder.AddAttribute(sequence++, "type", "button");
            builder.AddAttribute(sequence++, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, OnButtonClick));
            builder.AddContent(sequence++, "Alert");
            builder.CloseElement();
            builder.CloseElement();

            builder.OpenElement(sequence++, "td");
            builder.OpenElement(sequence++, "input");
            builder.AddAttribute(sequence++, "type", "checkbox");
            builder.AddAttribute(sequence++, "value", BindConverter.FormatValue(_checked));
            builder.CloseElement();
            builder.CloseElement();

            builder.CloseElement();
            
            builder.CloseElement();
        }
        
        private bool _checked = false;

        private async void OnButtonClick()
        {
            _checked = true;
            await _jsRuntime.InvokeVoidAsync("alert", "Hello");
            StateHasChanged();
        }
    }
}