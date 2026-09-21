using System.Windows;
using System.Windows.Controls;
using Recognition.Core;

namespace Recognition.Wpf;

public sealed class ParameterEditorTemplateSelector : DataTemplateSelector
{
    public DataTemplate? TextTemplate { get; set; }

    public DataTemplate? BooleanTemplate { get; set; }

    public DataTemplate? ChoiceTemplate { get; set; }

    public DataTemplate? NumericTemplate { get; set; }

    public DataTemplate? ImageTemplate { get; set; }

    public override DataTemplate? SelectTemplate(object item, DependencyObject container)
    {
        if (item is not ParameterEntryViewModel parameter)
        {
            return TextTemplate;
        }

        if (parameter.IsImagePath)
        {
            return ImageTemplate ?? TextTemplate;
        }

        return parameter.Definition.ValueKind switch
        {
            ParameterValueKind.Boolean => BooleanTemplate,
            ParameterValueKind.Choice => ChoiceTemplate,
            ParameterValueKind.Integer => NumericTemplate,
            ParameterValueKind.Decimal => NumericTemplate,
            _ => TextTemplate
        };
    }
}
