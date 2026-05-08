using System.Windows;
using System.Windows.Controls;
using FluentAssertions;
using ScaffoldX.App.Converters;
using ScaffoldX.App.Models;
using Xunit;

namespace ScaffoldX.App.Tests.Converters;

/// <summary>
/// Unit tests for <see cref="AnnotationTemplateSelector"/>.
/// All tests run on an STA thread because WPF DataTemplate/Button require it.
/// </summary>
public class AnnotationTemplateSelectorTests
{
    /// <summary>
    /// Runs the given action on an STA thread (required for WPF object creation).
    /// </summary>
    private static void RunOnStaThread(Action action)
    {
        Exception? caught = null;
        var thread = new Thread(() =>
        {
            try { action(); }
            catch (Exception ex) { caught = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (caught is not null)
            throw caught;
    }

    [Fact]
    public void SelectTemplate_BoundingBoxAnnotation_ReturnsBoundingBoxTemplate()
    {
        RunOnStaThread(() =>
        {
            var selector = new AnnotationTemplateSelector();
            var expected = new DataTemplate();
            selector.BoundingBoxTemplate = expected;
            var item = new BoundingBoxAnnotation();

            var result = selector.SelectTemplate(item, new Button());

            result.Should().BeSameAs(expected);
        });
    }

    [Fact]
    public void SelectTemplate_PolygonAnnotation_ReturnsPolygonTemplate()
    {
        RunOnStaThread(() =>
        {
            var selector = new AnnotationTemplateSelector();
            var expected = new DataTemplate();
            selector.PolygonTemplate = expected;
            var item = new PolygonAnnotation();

            var result = selector.SelectTemplate(item, new Button());

            result.Should().BeSameAs(expected);
        });
    }

    [Fact]
    public void SelectTemplate_OrientedBoundingBoxAnnotation_ReturnsObbTemplate()
    {
        RunOnStaThread(() =>
        {
            var selector = new AnnotationTemplateSelector();
            var expected = new DataTemplate();
            selector.OrientedBoundingBoxTemplate = expected;
            var item = new OrientedBoundingBoxAnnotation();

            var result = selector.SelectTemplate(item, new Button());

            result.Should().BeSameAs(expected);
        });
    }

    [Fact]
    public void SelectTemplate_SegmentationAnnotation_ReturnsSegmentationTemplate()
    {
        RunOnStaThread(() =>
        {
            var selector = new AnnotationTemplateSelector();
            var expected = new DataTemplate();
            selector.SegmentationTemplate = expected;
            var item = new SegmentationAnnotation();

            var result = selector.SelectTemplate(item, new Button());

            result.Should().BeSameAs(expected);
        });
    }

    [Fact]
    public void SelectTemplate_UnknownType_FallsBackToBase()
    {
        RunOnStaThread(() =>
        {
            var selector = new AnnotationTemplateSelector();
            var item = new object();

            var result = selector.SelectTemplate(item, new Button());

            // base.SelectTemplate returns null when no default is set
            result.Should().BeNull();
        });
    }

    [Fact]
    public void SelectTemplate_NullTemplate_ReturnsNull()
    {
        RunOnStaThread(() =>
        {
            var selector = new AnnotationTemplateSelector();
            // BoundingBoxTemplate is null by default
            var item = new BoundingBoxAnnotation();

            var result = selector.SelectTemplate(item, new Button());

            result.Should().BeNull();
        });
    }

    [Fact]
    public void SelectTemplate_NullItem_FallsBackToBase()
    {
        RunOnStaThread(() =>
        {
            var selector = new AnnotationTemplateSelector();

            var result = selector.SelectTemplate(null!, new Button());

            result.Should().BeNull();
        });
    }
}
