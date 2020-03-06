SELECT *
	FROM dbo.SmsEvents

SELECT COUNT(*) AS inProgress
  FROM [QMate].[dbo].[SmsEvents]
  WHERE State = 'IN_PROGRESS'

SELECT COUNT(*) AS New
  FROM [QMate].[dbo].[SmsEvents]
  WHERE State = 'NEW'

SELECT COUNT(*) AS Fail
  FROM [QMate].[dbo].[SmsEvents]
  WHERE State = 'FAIL'

SELECT COUNT(*) AS Sent
  FROM [QMate].[dbo].[SmsEvents]
  WHERE State = 'SENT'

  UPDATE [dbo].[SmsEvents]
	SET State = 'NEW'


DELETE 
  FROM [QMate].[dbo].[SmsEvents]


SELECT *
  FROM [QMate].[dbo].[SmsEvents]
  ORDER BY [SetTime] ASC

UPDATE dboSmsEvent
    SET State = 'IN_PROGRESS', LastUpdateTime = CURRENT_TIMESTAMP
        OUTPUT INSERTED.*
    FROM (SELECT TOP (10) * 
		FROM dbo.SmsEvents
        WHERE State = 'NEW'
        ORDER BY SetTime ASC) AS dboSmsEvent