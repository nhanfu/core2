create procedure [dbo].[usp_calcPayslip]
	@payslipId int,
	@ownerId int
as
begin
	declare @now datetime2 = getdate();
	declare @minWorkingDay int = 14;

	delete from PayslipDetail
	where PayslipId = @payslipId
	
	-- Calc basic salary first
	insert into PayslipDetail ([Month], [Year], PayslipId, UserId, WorkingDay, AdvancePayment, AmountPayback, InsuranceSalary,
		GrossSalary, IncomeTax, NetSalary, Commission, Allowance, Allowance2, Allowance3, Allowance4, Bonus1, Bonus2,
		Bonus3, Bonus4, Insurance1, Insurance2, Insurance3, Insurance4, CompanyInsurance1, CompanyInsurance2, 
		CompanyInsurance3, CompanyInsurance4, Active, InsertedBy, InsertedDate)

	select pay.[Month], pay.[Year], pay.Id, con.UserId, wd.TotalWorkingDayFull, 0 as AdvancePayment, 0 as AmountPayback,
		0 as InsuranceSalary,
		con.GrossSalary as GrossSalary,
		0 as IncomeTax, 0 as NetSalary, 0 as Commission, con.Allowance1, con.Allowance2, con.Allowance3, con.Allowance4,
		0 as Bonus1, 0 as Bonus2, 0 as Bonus3, 0 as Bonus4,
		iif(con.IsProbation = 1, 0, iif (wd.TotalWorkingDayFull > @minWorkingDay, con.InsuranceSalary * 0.08, 0)) as Insurance1,
		iif(con.IsProbation = 1, 0, iif (wd.TotalWorkingDayFull > @minWorkingDay, con.InsuranceSalary * 0.015, 0)) as Insurance2,
		iif(con.IsProbation = 1, 0, iif (wd.TotalWorkingDayFull > @minWorkingDay, con.InsuranceSalary * 0.01, 0)) as Insurance3,
		iif(con.IsProbation = 1, 0, iif (wd.TotalWorkingDayFull > @minWorkingDay, con.InsuranceSalary * 0.01, 0)) as Insurance4,
		iif(con.IsProbation = 1, 0, iif (wd.TotalWorkingDayFull > @minWorkingDay, con.InsuranceSalary * 0.175, 0)) as ComInsurance1,
		iif(con.IsProbation = 1, 0, iif (wd.TotalWorkingDayFull > @minWorkingDay, con.InsuranceSalary * 0.03, 0)) as ComInsurance3,
		iif(con.IsProbation = 1, 0, iif (wd.TotalWorkingDayFull > @minWorkingDay, con.InsuranceSalary * 0.01, 0)) as ComInsurance4,
		iif(con.IsProbation = 1, 0, iif (wd.TotalWorkingDayFull > @minWorkingDay, con.InsuranceSalary * 0.02, 0)) as ComInsurance4,
		1 as Active, @ownerId as InsertedBy, @now as InsertedDate
	from WorkingDayDetail as wd
	join Payslip as pay on wd.PayslipId = pay.Id
	cross apply dbo.GetContractByUser(wd.UserId) as con
	where wd.PayslipId = @payslipId
	
	-- calc payback
	update detail
	set detail.Active = 1, detail.AdvancePayment = payBack.AdvancedPayment, AmountPayback = payBack.AmountPayback
	from PayslipDetail as detail
	join
	(
		select back.UserId, sum(back.TotalAdvancedPayment) as AdvancedPayment, sum(back.TotalPaid - back.TotalReceived) as AmountPayback
		from PayslipDetail as detail
		join PaybackPayment as back on detail.UserId = back.UserId
		where back.HasPaid = 0 and back.SalaryDeduction = 1 and back.StatusId = 1 -- approved payback
		group by back.UserId
	) as payBack on detail.UserId = payBack.UserId
	where detail.PayslipId = @payslipId

	-- update payback status
	update PaybackPayment
	set HasPaid = 1
	where HasPaid = 0 and SalaryDeduction = 1 and StatusId = 1

	-- Calc income TAX and NET salary
	update detail
	set IncomeTax = iif(con.IsProbation = 1, iif(userIncome.IncomeWithTax < 2000000, 0, userIncome.IncomeWithTax * 0.1), userIncome.Tax)
	from PayslipDetail detail
	cross apply dbo.GetContractByUser(detail.UserId) as con
	join (
		select UserId, ABS(sum(Tax)) as Tax, IncomeWithTax from (
			select UserId, IncomeWithTax, dbo.CalcValueInRange(tax.[Min], tax.[Max], userTax.ShouldTax) * tax.[Percentage] / 100 as Tax
			from IncomeTax tax
			join (select UserId, iif(IncomeWithTax < FreeTax, 0, IncomeWithTax - FreeTax) as ShouldTax, IncomeWithTax from (
			select detail.UserId, detail.IncomeWithTax,
				count(fa.Id) * 4400000 + 11000000 as FreeTax
			from (
				select distinct wd.UserId, wd.PayslipId,
					wd.TotalWorkingDayFull / iif(wd.TotalWorkingDayFull = 0, 1, wd.TotalWorkingDayFull) * con.GrossSalary
						+ Commission + AmountPayback - AdvancePayment + Bonus1 + Bonus2 + Bonus3 + Bonus4 as IncomeWithTax
				from WorkingDayDetail wd
				join PayslipDetail detail on wd.PayslipId = detail.PayslipId
				join Payslip pay on wd.PayslipId = pay.Id
				cross apply dbo.GetContractByUser(wd.UserId) as con
			) as detail
			left join FamilyAllowances fa on detail.UserId = fa.UserId
			where detail.PayslipId = @payslipId and (fa.Active is null or fa.Active = 1)
			group by detail.UserId, detail.IncomeWithTax
			) as Deduction) as userTax on 1 = 1
			where tax.Active = 1 and tax.EffectiveDate < getdate() and (tax.ExpiredDate is null or tax.ExpiredDate > getdate())
		) as TaxByLevel
		group by UserId, IncomeWithTax
	) as userIncome on detail.UserId = userIncome.UserId
	where detail.PayslipId = @payslipId

	update PayslipDetail
	set NetSalary = GrossSalary - IncomeTax - Insurance1 - Insurance2 - Insurance3 - Insurance4
	where PayslipId = @payslipId
end
