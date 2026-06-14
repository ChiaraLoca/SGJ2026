# 4E: Le Menti — Design Carte

## Costi per Tier

| Tier | Costo Shop |
|------|------------|
| C    | 1 Nota     |
| B    | 3 Note     |
| A    | 10 Note    |

---

## Comandanti

### Storia (S)

| Campo                   | Valore                                                                      |
|-------------------------|-----------------------------------------------------------------------------|
| **Abilità Base**        | +3 Note all'inizio di ogni round per ogni Verifica giocata nella partita    |
| **Condizione Sblocco**  | Raggiungere 20 Crediti                                                      |
| **Abilità Secondaria**  | Raddoppia l'effetto di tutte le carte Studio giocate su di lui              |

### Matematica (M)

| Campo                   | Valore                                        |
|-------------------------|-----------------------------------------------|
| **Abilità Base**        | Inizi il round con 3 carte aggiuntive         |
| **Condizione Sblocco**  | Arrivare al 3° round                          |
| **Abilità Secondaria**  | Per ogni carta pescata, +1 Nota               |

### Inglese (I)

| Campo                   | Valore                                                                         |
|-------------------------|--------------------------------------------------------------------------------|
| **Abilità Base**        | Quando aumenti la Nota di Inglese, +1 Nota all'altro tuo comandante            |
| **Condizione Sblocco**  | Avere 15 carte nel mazzo                                                       |
| **Abilità Secondaria**  | Le carte che aumentano la Nota di Inglese vengono copiate sull'altro comandante |

### Educazione Fisica (E)

| Campo                   | Valore                                                                                         |
|-------------------------|-----------------------------------------------------------------------------------------------|
| **Abilità Base**        | Alla fine di ogni tuo turno, +1 Nota all'altro tuo comandante se hai meno carte in mano dell'avversario |
| **Condizione Sblocco**  | Arrivare a 0 Note su questo comandante                                                        |
| **Abilità Secondaria**  | Ogni volta che giochi un'azione, −1 Nota al comandante avversario con più Note                |

### Diritto (D)

| Campo                   | Valore                                                                                         |
|-------------------------|-----------------------------------------------------------------------------------------------|
| **Abilità Base**        | Alla fine di ogni tuo turno, +2 Note al comandante di Diritto se hai almeno una azione non usata |
| **Condizione Sblocco**  | Avere giocato 6 carte in un tuo turno                                                        |
| **Abilità Secondaria**  | Alla fine di ogni tuo turno, +2 Note al tuo secondo comandante (non quello di diritto) se hai almeno una azione non usata                |


### Arte (A)

| Campo                   | Valore                                                                                         |
|-------------------------|-----------------------------------------------------------------------------------------------|
| **Abilità Base**        | Se l'avversario gioca una carta con almeno un tag che è già stato giocato nel turno, -1 Nota al suo comandante con + Note |
| **Condizione Sblocco**  | Avere nel cimitero carte con un totale di 6 tag diversi                                                        |
| **Abilità Secondaria**  | Se una carta che giochi dovrebbe dare - Note ad un comandante avversario, le da anche all'altro                |


---

## Tag

| Tag            | Affinità Comandanti |
|----------------|---------------------|
| Pratica        | E, I                |
| Studio         | S, M                |
| Estetica       | S, E                |
| Letteratura    | S, I                |
| Pianificazione | M, E                |
| Ricerca        | M, I                |

---

## Carta Verifica

| Nome     | Tag | Effetto                                                                                                  | Target    |
|----------|-----|----------------------------------------------------------------------------------------------------------|-----------|
| Verifica | —   | Non può essere giocata il primo turno di ogni round. Termina il round. Trasforma la somma delle Note dei comandanti in crediti. | No target |

---

## Mazzo Storia

| Nome       | Tier | Qt | Tag               | Effetto                                                                                          | Target                                     |
|------------|------|----|-------------------|--------------------------------------------------------------------------------------------------|--------------------------------------------|
| Studio     | C    | 2  | Studio            | +2 Note al comandante                                                                            | Qualsiasi comandante                       |
| Dialogo    | C    | 1  | Letteratura       | Annulla il prossimo debuff ricevuto                                                              | No target                                  |
| Tutor      | B    | 1  | Estetica, Studio  | +4 Note al tuo comandante con Nota più bassa (tetto massimo: il valore del comandante più alto) | Automatico — tuo comandante con Nota più bassa |
| Wikipedia  | B    | 1  | Estetica, Letteratura | La prossima carta giocata dall'avversario (esclusa la Verifica) viene copiata e aggiunta immediatamente alla tua mano | No target                                  |

---

## Mazzo Matematica

| Nome       | Tier | Qt | Tag                       | Effetto                                   | Target               |
|------------|------|----|---------------------------|-------------------------------------------|----------------------|
| Metodo     | C    | 2  | Pianificazione            | +1 Nota; +1 Azione                        | Qualsiasi comandante |
| Studio     | C    | 1  | Studio                    | +2 Note al comandante                     | Qualsiasi comandante |
| Ripasso    | B    | 1  | Studio, Pianificazione    | +1 Nota per ogni carta nel cimitero       | Qualsiasi comandante |
| Biblioteca | B    | 1  | Ricerca  | Pesca carte fino ad averne 4 in mano      | No target            |

---

## Mazzo Educazione Fisica

| Nome           | Tier | Qt | Tag                      | Effetto                                                                                    | Target                                              |
|----------------|------|----|--------------------------|--------------------------------------------------------------------------------------------|-----------------------------------------------------|
| Minaccia       | C    | 2  | Pratica                  | −2 Note                                                                                    | Qualsiasi comandante                                |
| Metodo         | C    | 1  | Pianificazione           | +1 Nota; +1 Azione                                                                         | Qualsiasi comandante                                |
| Rissa          | B    | 1  | Estetica, Pratica        | −6 Note a un comandante avversario (a scelta); −4 Note a un tuo comandante (a scelta)      | 1 comandante avversario (a scelta) + 1 proprio (a scelta) |
| Test di Cooper | B    | 1  | Pianificazione, Estetica | +1 Azione; +2 Note al tuo comandante con Nota più bassa. Se la sua Nota era ≤ 3, riprendi questa carta in mano | Automatico — tuo comandante con Nota più bassa |

---

## Mazzo Inglese

| Nome       | Tier | Qt | Tag                  | Effetto                                                                  | Target               |
|------------|------|----|----------------------|--------------------------------------------------------------------------|----------------------|
| Dizionario | C    | 2  | Ricerca              | +1 Nota; pesca 1 carta                                                   | Qualsiasi comandante |
| Dialogo    | C    | 1  | Letteratura          | Annulla il prossimo debuff ricevuto                                      | No target            |
| Gossip     | B    | 1  | Ricerca, Pratica     | L'avversario scarta una carta a caso dalla mano (esclusa la Verifica)   | No target            |
| Fidanzata  | B    | 1  | Letteratura, Pratica | Fino all'inizio del tuo prossimo turno, le Note dei tuoi comandanti non possono calare | No target |

---

## Mazzo Diritto

| Nome       | Tier | Qt | Tag                  | Effetto                                                                  | Target               |
|------------|------|----|----------------------|--------------------------------------------------------------------------|----------------------|
| Obiezione! | C    | 2  | Pianificazione,Pratica              | -1 Nota; +1 Azione                                                 | Qualsiasi comandante |
| Metodo    | C    | 1  | Pianificazione  | +1 Nota; +1 Azione | Qualsiasi comandante |
| Codice Civile     | B    | 1  | Pianificazione    | +2 Note per ogni azione rimanente dopo aver giocato questa carta   | Qualsiasi Comandante            |
---
| Costituzione  | B    | 1  | Letteratura, Pratica | Fino all'inizio del tuo prossimo turno, annulla tutti i + Note e + Carte che  danno le carte giocate dal tuo avversario  | No target |

---

## Mazzo Arte

| Nome       | Tier | Qt | Tag                  | Effetto                                                                  | Target               |
|------------|------|----|----------------------|--------------------------------------------------------------------------|----------------------|
| Teoria dei Colori | C    | 2  | Estetica,Ricerca              | +1 Nota a qualsiasi comandante, - 1 Nota a qualsiasi comandante | Qualsiasi comandante, qualsiasi comandante |
| Dialogo    | C    | 1  | Letteratura       | Annulla il prossimo debuff ricevuto                                                              | No target                                  |
| Proporzioni     | B    | 1  | Studio, Estetica    | +1 Voto per ogni tag diverso tra le carte nel tuo cimitero   | Qualsiasi Comandante            |
| Dipinto  | B    | 1  | Pratica, Ricerca | Pesca una carta e -1 ad un comandante avversario per ogni tag diverso nella tua mano. costa 2 azioni           |
---

## Carte Shop

### Tier B (Costo: 3 Note)

| Nome           | Tag                              | Effetto                                                                  | Target               |
|----------------|----------------------------------|--------------------------------------------------------------------------|----------------------|
| Sabotaggio     | Pratica                          | −1 Nota per ogni carta Pratica nei cimiteri di entrambi i giocatori      | Qualsiasi comandante |
| Riassunto      | Ricerca, Studio                  | +1 Nota per ogni carta in mano                                           | Qualsiasi comandante |
| Schema         | Ricerca, Estetica                | Recupera una carta dal tuo cimitero; pesca 1 carta                       | No target            |
| Compito a Casa | Estetica                         | Metti 2 carte dal tuo cimitero in cima al mazzo                          | No target            |
| Progetto       | Studio, Pianificazione, Ricerca  | +1 carta; +1 Azione; +1 Nota                                             | Qualsiasi comandante |
| Presentazione  | Pratica, Pianificazione, Ricerca | +1 carta; +1 Azione; −1 Nota                                             | Qualsiasi comandante |
| Copiare        | Pianificazione, Estetica         | Riapplica l'effetto della prossima carta giocata in questo turno         | No target            |
| Sciopero       | Pianificazione, Pratica          | +1 Azione; l'avversario non può giocare la Verifica nel suo prossimo turno | No target          |

### Tier A (Costo: 10 Note)

| Nome                     | Tag                          | Effetto                                                                                                   | Target                                              |
|--------------------------|------------------------------|-----------------------------------------------------------------------------------------------------------|-----------------------------------------------------|
| Chat GPT                 | Pianificazione, Ricerca      | Pesca 2 carte; +2 azioni                                                                                    | Qualsiasi comandante                                |
| Politica                 | Letteratura                  | L'avversario scarta dalla mano tutte le carte Pratica                                                     | No target                                           |
| Approfondimento          | Ricerca                      | Pesca tutto il mazzo                                                                                      | No target                                           |
| Rappresentante di Classe | Estetica                     | Scambia le Note tra un tuo comandante e uno avversario (entrambi a scelta)                                | 1 comandante avversario (a scelta) + 1 proprio (a scelta) |
| Bullismo                 | Pratica                      | L'avversario scarta dalla mano tutte le carte Studio                                                      | No target                                           |
| Studio Notturno          | Studio                       | +10 Note a un comandante; costa 2 azioni                                                                  | Qualsiasi comandante                                |
| Appunti                  | Pianificazione               | +1 Nota e +1 Azione per ogni Tag distinto nel tuo cimitero                                                | Qualsiasi comandante                                |
| Occupazione              | Pratica, Estetica, Letteratura | Sposta la Verifica dell'avversario in fondo al suo mazzo                                                | No target                                           |
