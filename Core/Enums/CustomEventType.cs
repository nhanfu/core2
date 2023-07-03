namespace Core.Enums
{
    public enum CustomEventType
    {
        BeforeCopied,
        AfterWebsocket,
        AfterCopied,
        BeforeDeleted,
        AfterDeleted,
        Deactivated,
        BeforePasted,
        AfterPasted,
        BeforeCreated,
        AfterCreated,
        BeforeEmptyRowCreated,
        AfterEmptyRowCreated,
        BeforeCreatedList,
        AfterCreatedList,
        Selected,
        RowFocusIn,
        RowFocusOut,
        RowMouseEnter,
        RowMouseLeave,
        OpenRef,
        BeforePatchUpdate,
        ValidatePatchUpdate,
        BeforePatchCreate,
        AfterPatchUpdate,
    }

    public enum TypeEntityAction
    {
        UpdateEntity = 1,
        UpdateCountBadge = 2,
        Message = 3,
    }
}
