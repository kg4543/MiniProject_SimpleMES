/****** SSMS의 SelectTopNRows 명령 스크립트 ******/
SELECT [PrcIdx]
      ,[SchIdx]
      ,[PrcCD]
      ,[PrcDate]
      ,[PrcLoadTime]
      ,[PrcStartTime]
      ,[PrcEndTime]
      ,[PrcFacilityID]
      ,[PrcResult]
      ,[RegDate]
      ,[RegID]
      ,[ModDate]
      ,[ModID]
  FROM [MRP].[dbo].[Process]

  SELECT s.*, p.* FROM Schedules AS s
  INNER JOIN Process AS p
  ON s.SchIdx = p.SchIdx;

  -- PrcResult에서 성공갯수와 실패갯수를  다른 컬럼으로 분리
  SELECT p.SchIdx, p.PrcDate, 
	CASE p.PrcResult When 1 Then 1 END AS PrcOK,
	CASE p.PrcResult When 0 Then 1 END AS PrcFail
    FROM Process AS p

  -- 2. 합계집계
  SELECT smr.SchIdx, smr.PrcDate, sum(PrcOK) as OK_Amount, sum(PrcFail) as Fail_Amount
	From (
		  SELECT p.SchIdx, p.PrcDate, 
				CASE p.PrcResult When 1 Then 1 END AS PrcOK,
				CASE p.PrcResult When 0 Then 1 END AS PrcFail
		  FROM Process AS p
		  ) AS smr
	Group by smr.SchIdx, smr.PrcDate

  -- 3.0 join
  SELECT * From Schedules as sch
   inner join Process as prc
	  on sch.SchIdx = PrcIdx

  -- 3.1 2번결과(가상테이블)와 Schedules 테이블 조인해서 원하는 결과 도출
  SELECT sch.SchIdx, sch.PlantCode, sch.SchAmount, prc.PrcDate,
		 prc.OK_Amount, prc.Fail_Amount
	From Schedules as sch
   inner join(
			SELECT smr.SchIdx, smr.PrcDate, sum(PrcOK) as OK_Amount, sum(PrcFail) as Fail_Amount
			  From (
					SELECT p.SchIdx, p.PrcDate, 
							CASE p.PrcResult When 1 Then 1 END AS PrcOK,
							CASE p.PrcResult When 0 Then 1 END AS PrcFail
					FROM Process AS p
					) AS smr
			Group by smr.SchIdx, smr.PrcDate
			)AS prc
			 ON sch.SchIdx = prc.SchIdx
			 where sch.PlantCode = 'PC010002'
			   and prc.PrcDate Between '2021-06-30' and '2021-07-01' 
