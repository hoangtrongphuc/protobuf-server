CREATE TABLE NPCStat
(
    NPCStatID SERIAL CONSTRAINT PK_NPCStat PRIMARY KEY,
    NPCID INTEGER NOT NULL CONSTRAINT FK_NPCStat_NPC REFERENCES NPC,
    StatID INTEGER NOT NULL CONSTRAINT FK_NPCStat_Stat REFERENCES Stat,
    StatValue REAL NOT NULL
);