using B.BeaverTools.Utils.Extensions;

namespace B.BeaverTools.Models;

public class Tag(IndependentTag tag)
{
    //private XYZ _currentTagHeadPosition;

    public ElementId TagId { get; } = tag.Id;

    public IndependentTag IndependentTag { get; } = tag;
    public XYZ StartTagHeadPosition { get; } = tag.TagHeadPosition;
    public Outline StartTagOutline { get; } = tag.GetOutline();

    public XYZ CurrentTagHeadPosition
    {
        get => tag.TagHeadPosition;
    }

    // public XYZ CurrentTagHeadPosition
    // {
    //     get => _currentTagHeadPosition;
    //     set
    //     {
    //         _currentTagHeadPosition = value;
    //
    //         var baseMinPoint = StartTagOutline.MinimumPoint;
    //         var baseMaxPoint = StartTagOutline.MaximumPoint;
    //
    //         var moveVector = StartTagHeadPosition - _currentTagHeadPosition;
    //
    //         CurrentTagOutline = new Outline(
    //             baseMinPoint - moveVector,
    //             baseMaxPoint - moveVector);
    //     }
    // }

    public Outline CurrentTagOutline
    {
        get
        {
            var baseMinPoint = StartTagOutline.MinimumPoint;
            var baseMaxPoint = StartTagOutline.MaximumPoint;

            var moveVector = StartTagHeadPosition - tag.TagHeadPosition;

            return new Outline(
                baseMinPoint - moveVector,
                baseMaxPoint - moveVector);
        }
    }

    public Outline GetOutlineByPoint(XYZ point)
    {
        var baseMinPoint = StartTagOutline.MinimumPoint;
        var baseMaxPoint = StartTagOutline.MaximumPoint;

        var moveVector = StartTagHeadPosition - point;

        return new Outline(
            baseMinPoint - moveVector,
            baseMaxPoint - moveVector);
    }
}