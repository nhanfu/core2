
create function CalcValueInRange(@min decimal, @max decimal, @value decimal)
returns decimal
as
begin
return iif(@value < @min or @value = 0, 0, iif(@max is not null and @value > @max, @max, @min - @value));
end
