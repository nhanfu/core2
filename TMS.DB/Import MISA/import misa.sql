--select * from [FINAL$]
delete from [FINAL$] where NgayCT is null --or NgayCT = N'Ngày CT'

alter table [FINAL$] add Id int IDENTITY(1,1) not null
go

insert into PhieuThuChi (MALOAIPHIEU, MASO, DATEMODIFY, NGAY, PhieuThu, 
	Hinhthuc, Tygia, Donvi, SoTien, DienGiai, Nguoilap, PartnerID, RealPartnerID, SoTaiKhoan, CompID, GhiChu,
	Phieukhac, ChargesInside, ChargesOutside, CancelVc, Paid, VCLock, ReportTax, Roundable, CashChecked,
	ChangeEditLinked, LinkedDeleted, MarkImported)

select 
	case when SoCT like '%HTCCN%' or SoCT like '%HTNCN%' or SoCT like '%HTCPK%' then 'CONGNO' 
		 when SoCT like '%TBK%' or SoCT like '%UNC%' or SoCT like '%UNT%' then 'BNK' end as MALOAIPHIEU,
	SoCT as	MASO, getdate() as DATEMODIFY, NgayCT as NGAY,

	case when SoCT like '%HTNCN%' or SoCT like '%PT%' or SoCT like '%TBK%' or SoCT like '%UNT%' then 1
		 when SoCT like '%HTCCN%' or SoCT like '%PC%' or SoCT like '%UNC%' then 0 else 0 end as PhieuThu,

    case when SoCT like '%PT%' or SoCT like '%PC%' then N'Tiền mặt'
		 when SoCT like '%TBK%' or SoCT like '%UNC%' or SoCT like '%UNT%' then 'CK' end as Hinhthuc,

	1 as Tygia, TenDoiTuong as Donvi, TongTien as SoTien, DienGiai as DienGiai, Nguoilap,
	trim(MaDT) as PartnerID, MaDT as RealPartnerID, 

	case when SoCT like '%HTCCN%' then (case when [No] like '331%' then [No] else [Co] end)
		 when SoCT like '%HTNCN%' then (case when [No] like '131%' then [No] else [Co] end)
		 when SoCT like '%TBK%' or SoCT like '%UNC%' or SoCT like '%UNT%'  or SoCT like '%PT%' or SoCT like '%PC%' then (case when [No] like '11%'  then [No] else [Co] end)
		 end as SoTaiKhoan,
	'MLD/HCM' as CompID, SoHD as GhiChu, 
	0 as Phieukhac, 0 as ChargesInside, 0 as ChargesOutside, 0 as CancelVc, 0 as Paid,
	0 as VCLock, 0 as ReportTax, 0 as Roundable, 0 as CashChecked, 
	0 as ChangeEditLinked, 0 as LinkedDeleted, 0 as MarkImported
  from  [FINAL$] as misa
  join (
      select SoCT as Id, sum(cast(ThanhTien as decimal(20,5))) as TongTien
	  from [FINAL$]
	  group by SoCT
  ) as TongTien on misa.SoCT = TongTien.Id
  where misa.Id in (
    select Min(Id) from [FINAL$] group by SoCT
  )
  and MaDT in (
    select PartnerId from Partners
  )
  and misa.SoCT not in (select Maso from PhieuThuChi)

insert into PhieuthuchiDetail (MasoPhieu, Taikhoan, MaDonVi, SotienNoNT, SotienNo,
	SotienCoNT, SotienCo, DienGiai, Curr, TyGia, SIndex, GainLoss, OBH, InvoiceDate)

select * from (
	select ptc.Maso as MasoPhieu,
		case when SoCT like '%HTC%' or SoCT like '%HTCPK%' then [Co]
				when SoCT like '%HTN%' then [No]
				when SoCT like '%UNC%' OR SoCT like '%PC%' OR SoCT like 'HTCBK%' then [Co]
				when SoCT like '%TBK%' or SoCT like '%UNT%' or SoCT like '%PT%' then [No]
				end as Taikhoan,
		MaDT as MaDonVi, 
		case when SoCT like '%HTNCN%' or SoCT like '%PT%' or SoCT like '%UNT%' or SoCT like '%TBK%' then ThanhTien else null end as SotienNoNT,
		case when SoCT like '%HTNCN%' or SoCT like '%PT%' or SoCT like '%UNT%' or SoCT like '%TBK%' then ThanhTien else null end as SotienNo,

		case when SoCT like '%HTCCN%' or SoCT like '%PT%' OR SoCT like 'HTCBK%' then ThanhTien else null end as SotienCoNT,
		case when SoCT like '%HTCCN%' or SoCT like '%PT%' OR SoCT like 'HTCBK%' then ThanhTien else null end as SotienCo,
		misa.DienGiai, 'VND' as Curr, 1 as TyGia, ROW_NUMBER() over (ORDER BY SoCT ASC) as SIndex, null as GainLoss, 1 as OBH,
		NgayHD as InvoiceDate
	from [FINAL$] misa
	join PhieuThuChi ptc on misa.SoCT = ptc.Maso
) as A
where TaiKhoan is not null
 

insert into PhieuthuchiDetails (SoCT, KeyField, [Description], SoTKDU, Ngoaite, TienVND, Curr, TyGia, 
	SoHD, DoituongVAT, MSTVAT)

select ptc.Maso as SoCT, ROW_NUMBER() over (ORDER BY SoCT ASC) as KeyField,
	misa.DienGiai as [Description],
	case when SoCT like '%UNC%' OR SoCT like '%PC%' OR SoCT like 'HTCBK%' OR SoCT like '%HTC%' or SoCT like '%HTCPK%' then [No]
			when SoCT like '%TBK%' or SoCT like '%UNT%' or SoCT like '%PT%' OR SoCT like '%HTN%' then [Co]
			end as SoTKDU,
	ThanhTien as Ngoaite, ThanhTien as TienVND, 'VND' as Curr, 1 as TyGia, SoHD as SoHD, 
	TenDoiTuong as DoituongVAT, MaDT as MSTVAT
from [FINAL$] misa
join PhieuThuChi ptc on misa.SoCT = ptc.Maso
where SoCT != '#N/A'