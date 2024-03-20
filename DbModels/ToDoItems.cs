namespace SnapApp.Svc.DbModels
{
    public class ToDoItem
    {
        public int? Id { get; set; }
        public int? Order { get; set; }
        public required string Title { get; set; }
        public required string Url { get; set; }
        public bool? Completed { get; set; }
    }
}