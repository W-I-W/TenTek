public interface IInteractable
{
    // Вызывается при нажатии E
    void Interact();

    // Короткая подсказка в прицеле (можно вернуть string.Empty)
    string Hint { get; }
}
