-- =============================================================================
-- COUNTRY_TRANSLATION Duzeltme Scripti
-- Kapsam: TR, GB, AZ, DE, US, FR, ES, CN, SA
-- CALISTIRMA: Bu script DML icerdigi icin query-db.csx ile CALISTIRILMAZ.
--             DBeaver veya baska bir PostgreSQL istemcisiyle calistirin.
-- =============================================================================
-- Ulke ID sabitleri (dbo."COUNTRY", 2026-06-30):
--   AZ: 5c7ac52f-28e4-5526-9b4a-aea6713f9e5b
--   CN: e31d921b-0726-5586-b9bb-4f4a7097adc3
--   DE: 4c4dea15-e890-5a93-8273-ee0ca12c4854
--   ES: daa739ec-fa28-5cc0-86d6-b87693fb6f1e
--   FR: ba2f5dd5-90d2-56c0-88f8-83b4acf23fb8
--   GB: 36bc0ecc-8642-53c6-9ac0-550fac5aa351
--   SA: 5a9dfabc-62c8-5cbb-a8ab-f31d49f9de8a
--   TR: 26e5e16a-1199-54ce-bedf-4c652c979200
--   US: 7597a433-4bcc-5000-bd09-c9348114d108
-- Dil ID sabitleri (configuration.dbo."LANGUAGE"):
--   tr: c7b4e773-c3c7-5923-a931-d3f3d6fc66d3
--   en: cb22c1d7-bd52-5dd9-8b31-0031b4a53880
--   ar: df56d15e-ba49-53d3-b950-c4ce9df0e661
--   de: 1b4f36bb-08a8-4e86-a6d2-3a3f09a71bb3
--   ru: 6c2c103f-fe48-44fa-96b0-f1c4573eb66a
--   zh: aaa632ff-d69e-46c3-844b-7ea88e229d46
--   es: 4759c07d-9e22-44bd-ade4-fca41b1b5831
--   fr: 4c535b36-8f76-4b95-a7a8-8dfab562a216
--   pt: 877382b9-edf0-4924-9207-436329e6eb80
--   uz: 350eb5d1-da54-45fc-8737-231afdb965e8
--   az: 019d76d0-eaa4-7979-adba-a508de840b02
-- =============================================================================

BEGIN;

-- =============================================================================
-- ADIM 1: Orphan kayitlari sil
-- RECORD_ID'si aktif bir COUNTRY kaydina denk gelmeyen translation satirlari
-- (Hedef 9 ulkeyle sinirli)
-- =============================================================================

DELETE FROM dbo."COUNTRY_TRANSLATION"
WHERE "RECORD_ID" NOT IN (
    SELECT "ID" FROM dbo."COUNTRY" WHERE "DELETED" = false
);

-- =============================================================================
-- ADIM 2: Duplicate kayitlari sil (global - tum COUNTRY_TRANSLATION)
-- (RECORD_ID, COLUMN, LANGUAGE_ID) bazinda birden fazla satir varsa
-- UUID'si en kucuk olani birak, digerlerini sil.
-- Dil veya ulke kisiti yok: hangi dilde/ulkede olursa olsun temizlenir.
-- =============================================================================

DELETE FROM dbo."COUNTRY_TRANSLATION"
WHERE "ID" NOT IN (
    SELECT DISTINCT ON ("RECORD_ID", "COLUMN", "LANGUAGE_ID") "ID"
    FROM dbo."COUNTRY_TRANSLATION"
    ORDER BY "RECORD_ID", "COLUMN", "LANGUAGE_ID", "ID"
);

-- =============================================================================
-- ADIM 3: Upsert - 11 dil x 9 ulke = 99 hedef satir
-- Mantik: kayit varsa TRANSLATION'i guncelle, yoksa ekle.
-- CTE icindeki UPDATE + INSERT siralama garanti: UPDATE onceki snapshot'a gore
-- calisir, NOT EXISTS de ayni snapshot'u okur => cakisma olmaz.
-- =============================================================================

WITH target ("RECORD_ID", "LANGUAGE_ID", "TRANSLATION") AS (
    VALUES
    -- AZ
    ('5c7ac52f-28e4-5526-9b4a-aea6713f9e5b'::uuid, 'c7b4e773-c3c7-5923-a931-d3f3d6fc66d3'::uuid, 'Azerbaycan'),
    ('5c7ac52f-28e4-5526-9b4a-aea6713f9e5b'::uuid, 'cb22c1d7-bd52-5dd9-8b31-0031b4a53880'::uuid, 'Azerbaijan'),
    ('5c7ac52f-28e4-5526-9b4a-aea6713f9e5b'::uuid, 'df56d15e-ba49-53d3-b950-c4ce9df0e661'::uuid, 'أذربيجان'),
    ('5c7ac52f-28e4-5526-9b4a-aea6713f9e5b'::uuid, '1b4f36bb-08a8-4e86-a6d2-3a3f09a71bb3'::uuid, 'Aserbaidschan'),
    ('5c7ac52f-28e4-5526-9b4a-aea6713f9e5b'::uuid, '6c2c103f-fe48-44fa-96b0-f1c4573eb66a'::uuid, 'Азербайджан'),
    ('5c7ac52f-28e4-5526-9b4a-aea6713f9e5b'::uuid, 'aaa632ff-d69e-46c3-844b-7ea88e229d46'::uuid, '阿塞拜疆'),
    ('5c7ac52f-28e4-5526-9b4a-aea6713f9e5b'::uuid, '4759c07d-9e22-44bd-ade4-fca41b1b5831'::uuid, 'Azerbaiyán'),
    ('5c7ac52f-28e4-5526-9b4a-aea6713f9e5b'::uuid, '4c535b36-8f76-4b95-a7a8-8dfab562a216'::uuid, 'Azerbaïdjan'),
    ('5c7ac52f-28e4-5526-9b4a-aea6713f9e5b'::uuid, '877382b9-edf0-4924-9207-436329e6eb80'::uuid, 'Azerbaijão'),
    ('5c7ac52f-28e4-5526-9b4a-aea6713f9e5b'::uuid, '350eb5d1-da54-45fc-8737-231afdb965e8'::uuid, 'Ozarbayjon'),
    ('5c7ac52f-28e4-5526-9b4a-aea6713f9e5b'::uuid, '019d76d0-eaa4-7979-adba-a508de840b02'::uuid, 'Azərbaycan'),
    -- CN
    ('e31d921b-0726-5586-b9bb-4f4a7097adc3'::uuid, 'c7b4e773-c3c7-5923-a931-d3f3d6fc66d3'::uuid, 'Çin'),
    ('e31d921b-0726-5586-b9bb-4f4a7097adc3'::uuid, 'cb22c1d7-bd52-5dd9-8b31-0031b4a53880'::uuid, 'China'),
    ('e31d921b-0726-5586-b9bb-4f4a7097adc3'::uuid, 'df56d15e-ba49-53d3-b950-c4ce9df0e661'::uuid, 'الصين'),
    ('e31d921b-0726-5586-b9bb-4f4a7097adc3'::uuid, '1b4f36bb-08a8-4e86-a6d2-3a3f09a71bb3'::uuid, 'China'),
    ('e31d921b-0726-5586-b9bb-4f4a7097adc3'::uuid, '6c2c103f-fe48-44fa-96b0-f1c4573eb66a'::uuid, 'Китай'),
    ('e31d921b-0726-5586-b9bb-4f4a7097adc3'::uuid, 'aaa632ff-d69e-46c3-844b-7ea88e229d46'::uuid, '中国'),
    ('e31d921b-0726-5586-b9bb-4f4a7097adc3'::uuid, '4759c07d-9e22-44bd-ade4-fca41b1b5831'::uuid, 'China'),
    ('e31d921b-0726-5586-b9bb-4f4a7097adc3'::uuid, '4c535b36-8f76-4b95-a7a8-8dfab562a216'::uuid, 'Chine'),
    ('e31d921b-0726-5586-b9bb-4f4a7097adc3'::uuid, '877382b9-edf0-4924-9207-436329e6eb80'::uuid, 'China'),
    ('e31d921b-0726-5586-b9bb-4f4a7097adc3'::uuid, '350eb5d1-da54-45fc-8737-231afdb965e8'::uuid, 'Xitoy'),
    ('e31d921b-0726-5586-b9bb-4f4a7097adc3'::uuid, '019d76d0-eaa4-7979-adba-a508de840b02'::uuid, 'Çin'),
    -- DE
    ('4c4dea15-e890-5a93-8273-ee0ca12c4854'::uuid, 'c7b4e773-c3c7-5923-a931-d3f3d6fc66d3'::uuid, 'Almanya'),
    ('4c4dea15-e890-5a93-8273-ee0ca12c4854'::uuid, 'cb22c1d7-bd52-5dd9-8b31-0031b4a53880'::uuid, 'Germany'),
    ('4c4dea15-e890-5a93-8273-ee0ca12c4854'::uuid, 'df56d15e-ba49-53d3-b950-c4ce9df0e661'::uuid, 'ألمانيا'),
    ('4c4dea15-e890-5a93-8273-ee0ca12c4854'::uuid, '1b4f36bb-08a8-4e86-a6d2-3a3f09a71bb3'::uuid, 'Deutschland'),
    ('4c4dea15-e890-5a93-8273-ee0ca12c4854'::uuid, '6c2c103f-fe48-44fa-96b0-f1c4573eb66a'::uuid, 'Германия'),
    ('4c4dea15-e890-5a93-8273-ee0ca12c4854'::uuid, 'aaa632ff-d69e-46c3-844b-7ea88e229d46'::uuid, '德国'),
    ('4c4dea15-e890-5a93-8273-ee0ca12c4854'::uuid, '4759c07d-9e22-44bd-ade4-fca41b1b5831'::uuid, 'Alemania'),
    ('4c4dea15-e890-5a93-8273-ee0ca12c4854'::uuid, '4c535b36-8f76-4b95-a7a8-8dfab562a216'::uuid, 'Allemagne'),
    ('4c4dea15-e890-5a93-8273-ee0ca12c4854'::uuid, '877382b9-edf0-4924-9207-436329e6eb80'::uuid, 'Alemanha'),
    ('4c4dea15-e890-5a93-8273-ee0ca12c4854'::uuid, '350eb5d1-da54-45fc-8737-231afdb965e8'::uuid, 'Germaniya'),
    ('4c4dea15-e890-5a93-8273-ee0ca12c4854'::uuid, '019d76d0-eaa4-7979-adba-a508de840b02'::uuid, 'Almaniya'),
    -- ES
    ('daa739ec-fa28-5cc0-86d6-b87693fb6f1e'::uuid, 'c7b4e773-c3c7-5923-a931-d3f3d6fc66d3'::uuid, 'İspanya'),
    ('daa739ec-fa28-5cc0-86d6-b87693fb6f1e'::uuid, 'cb22c1d7-bd52-5dd9-8b31-0031b4a53880'::uuid, 'Spain'),
    ('daa739ec-fa28-5cc0-86d6-b87693fb6f1e'::uuid, 'df56d15e-ba49-53d3-b950-c4ce9df0e661'::uuid, 'إسبانيا'),
    ('daa739ec-fa28-5cc0-86d6-b87693fb6f1e'::uuid, '1b4f36bb-08a8-4e86-a6d2-3a3f09a71bb3'::uuid, 'Spanien'),
    ('daa739ec-fa28-5cc0-86d6-b87693fb6f1e'::uuid, '6c2c103f-fe48-44fa-96b0-f1c4573eb66a'::uuid, 'Испания'),
    ('daa739ec-fa28-5cc0-86d6-b87693fb6f1e'::uuid, 'aaa632ff-d69e-46c3-844b-7ea88e229d46'::uuid, '西班牙'),
    ('daa739ec-fa28-5cc0-86d6-b87693fb6f1e'::uuid, '4759c07d-9e22-44bd-ade4-fca41b1b5831'::uuid, 'España'),
    ('daa739ec-fa28-5cc0-86d6-b87693fb6f1e'::uuid, '4c535b36-8f76-4b95-a7a8-8dfab562a216'::uuid, 'Espagne'),
    ('daa739ec-fa28-5cc0-86d6-b87693fb6f1e'::uuid, '877382b9-edf0-4924-9207-436329e6eb80'::uuid, 'Espanha'),
    ('daa739ec-fa28-5cc0-86d6-b87693fb6f1e'::uuid, '350eb5d1-da54-45fc-8737-231afdb965e8'::uuid, 'Ispaniya'),
    ('daa739ec-fa28-5cc0-86d6-b87693fb6f1e'::uuid, '019d76d0-eaa4-7979-adba-a508de840b02'::uuid, 'İspaniya'),
    -- FR
    ('ba2f5dd5-90d2-56c0-88f8-83b4acf23fb8'::uuid, 'c7b4e773-c3c7-5923-a931-d3f3d6fc66d3'::uuid, 'Fransa'),
    ('ba2f5dd5-90d2-56c0-88f8-83b4acf23fb8'::uuid, 'cb22c1d7-bd52-5dd9-8b31-0031b4a53880'::uuid, 'France'),
    ('ba2f5dd5-90d2-56c0-88f8-83b4acf23fb8'::uuid, 'df56d15e-ba49-53d3-b950-c4ce9df0e661'::uuid, 'فرنسا'),
    ('ba2f5dd5-90d2-56c0-88f8-83b4acf23fb8'::uuid, '1b4f36bb-08a8-4e86-a6d2-3a3f09a71bb3'::uuid, 'Frankreich'),
    ('ba2f5dd5-90d2-56c0-88f8-83b4acf23fb8'::uuid, '6c2c103f-fe48-44fa-96b0-f1c4573eb66a'::uuid, 'Франция'),
    ('ba2f5dd5-90d2-56c0-88f8-83b4acf23fb8'::uuid, 'aaa632ff-d69e-46c3-844b-7ea88e229d46'::uuid, '法国'),
    ('ba2f5dd5-90d2-56c0-88f8-83b4acf23fb8'::uuid, '4759c07d-9e22-44bd-ade4-fca41b1b5831'::uuid, 'Francia'),
    ('ba2f5dd5-90d2-56c0-88f8-83b4acf23fb8'::uuid, '4c535b36-8f76-4b95-a7a8-8dfab562a216'::uuid, 'France'),
    ('ba2f5dd5-90d2-56c0-88f8-83b4acf23fb8'::uuid, '877382b9-edf0-4924-9207-436329e6eb80'::uuid, 'França'),
    ('ba2f5dd5-90d2-56c0-88f8-83b4acf23fb8'::uuid, '350eb5d1-da54-45fc-8737-231afdb965e8'::uuid, 'Fransiya'),
    ('ba2f5dd5-90d2-56c0-88f8-83b4acf23fb8'::uuid, '019d76d0-eaa4-7979-adba-a508de840b02'::uuid, 'Fransa'),
    -- GB
    ('36bc0ecc-8642-53c6-9ac0-550fac5aa351'::uuid, 'c7b4e773-c3c7-5923-a931-d3f3d6fc66d3'::uuid, 'Birleşik Krallık'),
    ('36bc0ecc-8642-53c6-9ac0-550fac5aa351'::uuid, 'cb22c1d7-bd52-5dd9-8b31-0031b4a53880'::uuid, 'United Kingdom'),
    ('36bc0ecc-8642-53c6-9ac0-550fac5aa351'::uuid, 'df56d15e-ba49-53d3-b950-c4ce9df0e661'::uuid, 'المملكة المتحدة'),
    ('36bc0ecc-8642-53c6-9ac0-550fac5aa351'::uuid, '1b4f36bb-08a8-4e86-a6d2-3a3f09a71bb3'::uuid, 'Vereinigtes Königreich'),
    ('36bc0ecc-8642-53c6-9ac0-550fac5aa351'::uuid, '6c2c103f-fe48-44fa-96b0-f1c4573eb66a'::uuid, 'Великобритания'),
    ('36bc0ecc-8642-53c6-9ac0-550fac5aa351'::uuid, 'aaa632ff-d69e-46c3-844b-7ea88e229d46'::uuid, '英国'),
    ('36bc0ecc-8642-53c6-9ac0-550fac5aa351'::uuid, '4759c07d-9e22-44bd-ade4-fca41b1b5831'::uuid, 'Reino Unido'),
    ('36bc0ecc-8642-53c6-9ac0-550fac5aa351'::uuid, '4c535b36-8f76-4b95-a7a8-8dfab562a216'::uuid, 'Royaume-Uni'),
    ('36bc0ecc-8642-53c6-9ac0-550fac5aa351'::uuid, '877382b9-edf0-4924-9207-436329e6eb80'::uuid, 'Reino Unido'),
    ('36bc0ecc-8642-53c6-9ac0-550fac5aa351'::uuid, '350eb5d1-da54-45fc-8737-231afdb965e8'::uuid, 'Birlashgan Qirollik'),
    ('36bc0ecc-8642-53c6-9ac0-550fac5aa351'::uuid, '019d76d0-eaa4-7979-adba-a508de840b02'::uuid, 'Birləşmiş Krallıq'),
    -- SA
    ('5a9dfabc-62c8-5cbb-a8ab-f31d49f9de8a'::uuid, 'c7b4e773-c3c7-5923-a931-d3f3d6fc66d3'::uuid, 'Suudi Arabistan'),
    ('5a9dfabc-62c8-5cbb-a8ab-f31d49f9de8a'::uuid, 'cb22c1d7-bd52-5dd9-8b31-0031b4a53880'::uuid, 'Saudi Arabia'),
    ('5a9dfabc-62c8-5cbb-a8ab-f31d49f9de8a'::uuid, 'df56d15e-ba49-53d3-b950-c4ce9df0e661'::uuid, 'المملكة العربية السعودية'),
    ('5a9dfabc-62c8-5cbb-a8ab-f31d49f9de8a'::uuid, '1b4f36bb-08a8-4e86-a6d2-3a3f09a71bb3'::uuid, 'Saudi-Arabien'),
    ('5a9dfabc-62c8-5cbb-a8ab-f31d49f9de8a'::uuid, '6c2c103f-fe48-44fa-96b0-f1c4573eb66a'::uuid, 'Саудовская Аравия'),
    ('5a9dfabc-62c8-5cbb-a8ab-f31d49f9de8a'::uuid, 'aaa632ff-d69e-46c3-844b-7ea88e229d46'::uuid, '沙特阿拉伯'),
    ('5a9dfabc-62c8-5cbb-a8ab-f31d49f9de8a'::uuid, '4759c07d-9e22-44bd-ade4-fca41b1b5831'::uuid, 'Arabia Saudita'),
    ('5a9dfabc-62c8-5cbb-a8ab-f31d49f9de8a'::uuid, '4c535b36-8f76-4b95-a7a8-8dfab562a216'::uuid, 'Arabie saoudite'),
    ('5a9dfabc-62c8-5cbb-a8ab-f31d49f9de8a'::uuid, '877382b9-edf0-4924-9207-436329e6eb80'::uuid, 'Arábia Saudita'),
    ('5a9dfabc-62c8-5cbb-a8ab-f31d49f9de8a'::uuid, '350eb5d1-da54-45fc-8737-231afdb965e8'::uuid, 'Saudiya Arabistoni'),
    ('5a9dfabc-62c8-5cbb-a8ab-f31d49f9de8a'::uuid, '019d76d0-eaa4-7979-adba-a508de840b02'::uuid, 'Səudiyyə Ərəbistanı'),
    -- TR
    ('26e5e16a-1199-54ce-bedf-4c652c979200'::uuid, 'c7b4e773-c3c7-5923-a931-d3f3d6fc66d3'::uuid, 'Türkiye'),
    ('26e5e16a-1199-54ce-bedf-4c652c979200'::uuid, 'cb22c1d7-bd52-5dd9-8b31-0031b4a53880'::uuid, 'Turkey'),
    ('26e5e16a-1199-54ce-bedf-4c652c979200'::uuid, 'df56d15e-ba49-53d3-b950-c4ce9df0e661'::uuid, 'تركيا'),
    ('26e5e16a-1199-54ce-bedf-4c652c979200'::uuid, '1b4f36bb-08a8-4e86-a6d2-3a3f09a71bb3'::uuid, 'Türkei'),
    ('26e5e16a-1199-54ce-bedf-4c652c979200'::uuid, '6c2c103f-fe48-44fa-96b0-f1c4573eb66a'::uuid, 'Турция'),
    ('26e5e16a-1199-54ce-bedf-4c652c979200'::uuid, 'aaa632ff-d69e-46c3-844b-7ea88e229d46'::uuid, '土耳其'),
    ('26e5e16a-1199-54ce-bedf-4c652c979200'::uuid, '4759c07d-9e22-44bd-ade4-fca41b1b5831'::uuid, 'Turquía'),
    ('26e5e16a-1199-54ce-bedf-4c652c979200'::uuid, '4c535b36-8f76-4b95-a7a8-8dfab562a216'::uuid, 'Turquie'),
    ('26e5e16a-1199-54ce-bedf-4c652c979200'::uuid, '877382b9-edf0-4924-9207-436329e6eb80'::uuid, 'Turquia'),
    ('26e5e16a-1199-54ce-bedf-4c652c979200'::uuid, '350eb5d1-da54-45fc-8737-231afdb965e8'::uuid, 'Turkiya'),
    ('26e5e16a-1199-54ce-bedf-4c652c979200'::uuid, '019d76d0-eaa4-7979-adba-a508de840b02'::uuid, 'Türkiyə'),
    -- US
    ('7597a433-4bcc-5000-bd09-c9348114d108'::uuid, 'c7b4e773-c3c7-5923-a931-d3f3d6fc66d3'::uuid, 'Amerika Birleşik Devletleri'),
    ('7597a433-4bcc-5000-bd09-c9348114d108'::uuid, 'cb22c1d7-bd52-5dd9-8b31-0031b4a53880'::uuid, 'United States'),
    ('7597a433-4bcc-5000-bd09-c9348114d108'::uuid, 'df56d15e-ba49-53d3-b950-c4ce9df0e661'::uuid, 'الولايات المتحدة'),
    ('7597a433-4bcc-5000-bd09-c9348114d108'::uuid, '1b4f36bb-08a8-4e86-a6d2-3a3f09a71bb3'::uuid, 'Vereinigte Staaten'),
    ('7597a433-4bcc-5000-bd09-c9348114d108'::uuid, '6c2c103f-fe48-44fa-96b0-f1c4573eb66a'::uuid, 'Соединённые Штаты'),
    ('7597a433-4bcc-5000-bd09-c9348114d108'::uuid, 'aaa632ff-d69e-46c3-844b-7ea88e229d46'::uuid, '美国'),
    ('7597a433-4bcc-5000-bd09-c9348114d108'::uuid, '4759c07d-9e22-44bd-ade4-fca41b1b5831'::uuid, 'Estados Unidos'),
    ('7597a433-4bcc-5000-bd09-c9348114d108'::uuid, '4c535b36-8f76-4b95-a7a8-8dfab562a216'::uuid, 'États-Unis'),
    ('7597a433-4bcc-5000-bd09-c9348114d108'::uuid, '877382b9-edf0-4924-9207-436329e6eb80'::uuid, 'Estados Unidos'),
    ('7597a433-4bcc-5000-bd09-c9348114d108'::uuid, '350eb5d1-da54-45fc-8737-231afdb965e8'::uuid, 'Amerika Qoʻshma Shtatlari'),
    ('7597a433-4bcc-5000-bd09-c9348114d108'::uuid, '019d76d0-eaa4-7979-adba-a508de840b02'::uuid, 'Amerika Birləşmiş Ştatları')
),
updated AS (
    UPDATE dbo."COUNTRY_TRANSLATION" ct
    SET "TRANSLATION" = t."TRANSLATION"
    FROM target t
    WHERE ct."RECORD_ID"   = t."RECORD_ID"
      AND ct."COLUMN"      = 'Name'
      AND ct."LANGUAGE_ID" = t."LANGUAGE_ID"
      AND ct."TRANSLATION" IS DISTINCT FROM t."TRANSLATION"
    RETURNING ct."RECORD_ID", ct."LANGUAGE_ID"
)
INSERT INTO dbo."COUNTRY_TRANSLATION" ("ID", "CORRELATION_ID", "RECORD_ID", "COLUMN", "LANGUAGE_ID", "TRANSLATION")
SELECT
    gen_random_uuid(),
    gen_random_uuid(),
    t."RECORD_ID",
    'Name',
    t."LANGUAGE_ID",
    t."TRANSLATION"
FROM target t
WHERE NOT EXISTS (
    SELECT 1
    FROM dbo."COUNTRY_TRANSLATION" ct
    WHERE ct."RECORD_ID"   = t."RECORD_ID"
      AND ct."COLUMN"      = 'Name'
      AND ct."LANGUAGE_ID" = t."LANGUAGE_ID"
);

COMMIT;

-- =============================================================================
-- DOGRULAMA SORGUSU (query-db.csx ile calistirin)
-- Beklenen: her ulke icin TOTAL_ROWS=11, DISTINCT_LANGS=11, DUPLICATES=0
-- =============================================================================
-- SELECT c."CODE",
--        COUNT(ct."ID")                   AS "TOTAL_ROWS",
--        COUNT(DISTINCT ct."LANGUAGE_ID") AS "DISTINCT_LANGS",
--        COUNT(ct."ID") - COUNT(DISTINCT ct."LANGUAGE_ID") AS "DUPLICATES"
-- FROM dbo."COUNTRY" c
-- LEFT JOIN dbo."COUNTRY_TRANSLATION" ct ON ct."RECORD_ID" = c."ID" AND ct."COLUMN" = 'Name'
-- WHERE c."DELETED" = false AND c."CODE" IN ('TR','GB','AZ','DE','US','FR','ES','CN','SA')
-- GROUP BY c."ID", c."CODE" ORDER BY c."CODE";
