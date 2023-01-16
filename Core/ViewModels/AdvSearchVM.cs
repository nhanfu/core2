using Core.Models;
using Core.Enums;
using System.Collections.Generic;

namespace Core.Models
{
    public class AdvSearchVM
    {
        public ActiveStateEnum ActiveState { get; set; }
        public List<FieldCondition> Conditions { get; set; }
        public List<OrderBy> OrderBy { get; set; }
        public AdvSearchVM()
        {
            Conditions = new List<FieldCondition>();
            OrderBy = new List<OrderBy>();
        }
    }

    public class CellSelected
    {
        public string FieldName { get; set; }
        public string FieldText { get; set; }
        public string ComponentType { get; set; }
        public string Value { get; set; }
        public string ValueText { get; set; }
        public string Operator { get; set; }
        public string OperatorText { get; set; }
        public LogicOperation? Logic { get; set; }
        public bool IsSearch { get; set; }
    }

    public class FieldCondition
    {
        public int Id { get; set; }
        public int FieldId { get; set; }
        public GridPolicy Field { get; set; }
        public AdvSearchOperation? CompareOperatorId { get; set; }
        public string Value { get; set; }
        public LogicOperation? LogicOperatorId { get; set; }
        public Entity LogicOperator { get; set; }
        public int Level { get; set; }
    }

    public class OrderBy
    {
        public int Id { get; set; }
        public int FieldId { get; set; }
        public GridPolicy Field { get; set; }
        public OrderbyOption? OrderbyOptionId { get; set; }
    }
}
