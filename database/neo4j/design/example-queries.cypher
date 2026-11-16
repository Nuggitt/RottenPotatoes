// Eksempel-queries mod den migrerede graf (databasen `neo4j`).
// Kør i Neo4j Browser: http://localhost:7474 (bruger neo4j, password fra .env).
// Design: docs/database-mapping.md, afsnit 3.

// Gennemsnitlig score og antal ratings for én film (gemmes ikke, beregnes)
MATCH (m:Movie {id: 740})<-[:RATES]-(r:Rating)
RETURN m.title, round(avg(r.score), 2) AS gennemsnit, count(r) AS antal;

// Skuespillere, der oftest har spillet sammen med en given skuespiller
MATCH (:Actor {name: "Robert De Niro"})-[:ACTED_IN]->(m)<-[:ACTED_IN]-(co:Actor)
RETURN co.name, count(m) AS fælles ORDER BY fælles DESC LIMIT 10;

// "Brugere der gav film 740 mindst 8, gav også mindst 8 til ..."
MATCH (:Movie {id: 740})<-[:RATES]-(r1:Rating)<-[:GAVE]-(u:User)-[:GAVE]->(r2:Rating)-[:RATES]->(other:Movie)
WHERE r1.score >= 8 AND r2.score >= 8 AND other.id <> 740
RETURN other.title, count(DISTINCT u) AS brugere ORDER BY brugere DESC LIMIT 10;

// En brugers ratings med review
MATCH (u:User {id: 3368})-[:GAVE]->(r:Rating)-[:RATES]->(t)
OPTIONAL MATCH (r)-[:HAS_REVIEW]->(rev:Review)
RETURN labels(t)[0] AS type, t.title, r.score, rev.content
ORDER BY r.createdAt DESC LIMIT 10;

// Mest populære genrer målt på antal ratings
MATCH (g:Genre)<-[:IN_GENRE]-(t)<-[:RATES]-(r:Rating)
RETURN g.name, count(r) AS ratings ORDER BY ratings DESC LIMIT 10;
