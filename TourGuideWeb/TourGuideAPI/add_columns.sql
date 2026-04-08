-- Add missing columns to Places table
-- This SQL script adds TtsScript and Radius columns without affecting other columns

ALTER TABLE "Places" 
ADD COLUMN IF NOT EXISTS "TtsScript" text;

ALTER TABLE "Places"
ADD COLUMN IF NOT EXISTS "Radius" double precision DEFAULT 100.0;

-- Verify the columns were added
SELECT column_name, data_type, column_default 
FROM information_schema.columns 
WHERE table_name = 'Places' 
AND column_name IN ('TtsScript', 'Radius');
