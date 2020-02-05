using System.ComponentModel.DataAnnotations;

namespace NCNEPortal.Enums
{
    public enum DeadlineEnum
    {
        [Display(Name = "Two Weeks")]
        TwoWeeks = 1,
        [Display(Name = "Three Weeks")]
        ThreeWeeks = 2
    }
}
