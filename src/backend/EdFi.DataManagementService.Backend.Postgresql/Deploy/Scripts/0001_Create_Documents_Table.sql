-- SPDX-License-Identifier: Apache-2.0
-- Licensed to the Ed-Fi Alliance under one or more agreements.
-- The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
-- See the LICENSE and NOTICES files in the project root for more information.

CREATE TABLE Documents (
  Id BIGINT GENERATED ALWAYS AS IDENTITY(START WITH 1 INCREMENT BY 1),
  DocumentPartitionKey SMALLINT NOT NULL,
  DocumentUuid UUID NOT NULL,
  ResourceName VARCHAR(256) NOT NULL,
  ResourceVersion VARCHAR(64) NOT NULL,
  ProjectName VARCHAR(256) NOT NULL,
  EdfiDoc JSONB NOT NULL,
  CreatedAt TIMESTAMP NOT NULL DEFAULT NOW(),
  LastModifiedAt TIMESTAMP NOT NULL DEFAULT NOW(),
  PRIMARY KEY (DocumentPartitionKey, Id)
) PARTITION BY HASH(DocumentPartitionKey);

CREATE TABLE Documents_00 PARTITION OF Documents FOR VALUES WITH (MODULUS 16, REMAINDER 0);
CREATE TABLE Documents_01 PARTITION OF Documents FOR VALUES WITH (MODULUS 16, REMAINDER 1);
CREATE TABLE Documents_02 PARTITION OF Documents FOR VALUES WITH (MODULUS 16, REMAINDER 2);
CREATE TABLE Documents_03 PARTITION OF Documents FOR VALUES WITH (MODULUS 16, REMAINDER 3);
CREATE TABLE Documents_04 PARTITION OF Documents FOR VALUES WITH (MODULUS 16, REMAINDER 4);
CREATE TABLE Documents_05 PARTITION OF Documents FOR VALUES WITH (MODULUS 16, REMAINDER 5);
CREATE TABLE Documents_06 PARTITION OF Documents FOR VALUES WITH (MODULUS 16, REMAINDER 6);
CREATE TABLE Documents_07 PARTITION OF Documents FOR VALUES WITH (MODULUS 16, REMAINDER 7);
CREATE TABLE Documents_08 PARTITION OF Documents FOR VALUES WITH (MODULUS 16, REMAINDER 8);
CREATE TABLE Documents_09 PARTITION OF Documents FOR VALUES WITH (MODULUS 16, REMAINDER 9);
CREATE TABLE Documents_10 PARTITION OF Documents FOR VALUES WITH (MODULUS 16, REMAINDER 10);
CREATE TABLE Documents_11 PARTITION OF Documents FOR VALUES WITH (MODULUS 16, REMAINDER 11);
CREATE TABLE Documents_12 PARTITION OF Documents FOR VALUES WITH (MODULUS 16, REMAINDER 12);
CREATE TABLE Documents_13 PARTITION OF Documents FOR VALUES WITH (MODULUS 16, REMAINDER 13);
CREATE TABLE Documents_14 PARTITION OF Documents FOR VALUES WITH (MODULUS 16, REMAINDER 14);
CREATE TABLE Documents_15 PARTITION OF Documents FOR VALUES WITH (MODULUS 16, REMAINDER 15);

-- GET/UPDATE/DELETE by id lookup support, DocumentUuid uniqueness validation
CREATE UNIQUE INDEX UX_Documents_DocumentUuid ON Documents(DocumentPartitionKey, DocumentUuid)
