#João Pedro Pereira Esteves ist178304 group 095
import numpy as np
import math
import itertools
import random
from operator import itemgetter

def findNE(bM):

    player1 = np.full((bM.shape[0], 2), ([math.inf, math.inf], -math.inf))#np.array([([math.inf, math.inf], -math.inf),([math.inf, math.inf], -math.inf)])  #[(pos, cost), (pos, cost)]
    player2 = np.full((bM.shape[0], 2), ([math.inf, math.inf], -math.inf))#np.array([([math.inf, math.inf], -math.inf),([math.inf, math.inf], -math.inf)])

    for x in range(0, bM.shape[1]):
        for y in range(0, bM[:,x].shape[0]):
            if bM[:,x][y][0] > player1[x][1]:
                player1[x][0] = (y, x)
                player1[x][1] = bM[:,x][y][0]

    for x in range(0, bM.shape[0]):
        for y in range(0, bM[x,:].shape[0]):
            if bM[x,:][y][1] > player2[x][1]:
                player2[x][0] = (x, y)
                player2[x][1] = bM[x,:][y][1]

    ne = []
    for x in range(0, player1.shape[0]):
        for y in range(0, player2.shape[0]):
            if player1[x][0] == player2[y][0]:
                ne.append(list(player1[x][0]))
    nev = []
    for x in ne:
        nev.append(list(bM[x[0], x[1]]))

    return ne, nev


def findmixedNE(M):

    ne = np.array([[-1.,-1.],[-1.,-1.]])

    if ((M[0][0][0] - M[0][1][0] - M[1][0][0] + M[1][1][0])) != 0:
        p = (M[1][1][0] - M[0][1][0]) / ((M[0][0][0] - M[0][1][0] - M[1][0][0] + M[1][1][0]))
        if (p > 1) or (p < 0):
            p = -1
        else:
            ne[1] = [p, 1 - p]

    if ((M[0][0][1] - M[1][0][1] - M[0][1][1] + M[1][1][1])) != 0:
        q = (M[1][1][1] - M[1][0][1]) / ((M[0][0][1] - M[1][0][1] - M[0][1][1] + M[1][1][1]))
        if (q > 1) or (q < 0):
            q = -1
        else:
            ne[0] = [q, 1 - q]

    return ne

def payPromiseE(M):

    initialNE, initialNEV = findNE(M)
    ne, nev, sp = [], [], []

    for nash in initialNEV:
        for y in range(0, M.shape[1]): #y row
            for x in range(0, M[:,y].shape[0]): #x line
                if [x, y] in initialNE:
                    continue;

                if M[x][y][0] > nash[0]:
                    value = (M[x][y][0] - M[x][y][1]) / 2
                    if negotiate(1, M[x][y], nash, value, initialNEV):
                        ne.append([x,y])
                        nev.append([M[x][y][0] - value ,M[x][y][1] + value])
                        sp.append(abs(value))
                        if value <= 0:
                            continue

                if M[x][y][1] > nash[1]:
                    value = (M[x][y][1] - M[x][y][0]) / 2
                    if negotiate(0, M[x][y], nash, value, initialNEV):
                        if value <= 0:
                            continue
                        ne.append([x,y])
                        nev.append([M[x][y][0] + value ,M[x][y][1] - value])
                        sp.append(abs(value))


    return ne, nev, sp


def negotiate(player, offer, start, value, nashList):

    propose = []
    propose.append(offer[0])
    propose.append(offer[1])
    propose[player] += value
    if player == 0:
        propose[1] -= value
    else:
        propose[0] -= value

    if (propose[player] >= start[player]):
        for nash in nashList:
            if (propose[0] + propose[1]) < (nash[0] + nash[1]):
                return False
        return True

    return False


def utilities(utility1, utility2, offer1, offer2):

        u1, u2, u3, u4 = 0, 0, 0, 0
        for x in offer2[0][0]:
            u1 += utility1[x]
        for x in offer1[0][0]:
            u2 += utility1[x]
        for x in offer1[0][1]:
            u3 += utility2[x]
        for x in offer2[0][1]:
            u4 += utility2[x]

        return u1, u2, u3, u4

def calculateOffer(playerUtil, oponentUtil, lastOffer, player, lastOfferValue):

    combinations = []
    indexes = list(range(len(playerUtil)))

    if player == 2:
        position = 0
        oposite = 1
    else:
        position = 1
        oposite = 0

    oldCost = 0
    for x in lastOffer[0][position]:
        oldCost += playerUtil[x]

    #calculate possible negotiations
    for i in range(1, len(playerUtil)):
        combinations = combinations + (list(itertools.combinations(indexes, i)))

    #from the possible negotiations, chose those wich are better than the last one
    possibleActions = []
    for x in combinations:
        myutil = sum([playerUtil[index] for index in x])
        hisutil = sum([oponentUtil[index] for index in x])
        possibleActions.append([x, (myutil, hisutil)])

    #from the better offers, chose the one that minimizes the cost for the player, and maximizes the earnings of the oponent
    minimum = [(0, 0), (math.inf, 0)]
    for x in possibleActions:
        if minimum[1][0] >= x[1][0] and x[1][0] > oldCost:
            if x[1][1] >= minimum[1][1] and x[1][1] > lastOfferValue :
                minimum = x

    aux = []
    for x in range(0, len(indexes)):
        if x not in minimum[0]:
            aux.append(x)

    #create and return the offer
    if player == 2:
        return [list(minimum[0]), aux]
    else:
        return [aux, list(minimum[0])]





def zeuthenStep(utility1, utility2, lastOffer1, lastOffer2):
    u1, u2, u3, u4 = utilities(utility1, utility2, lastOffer1, lastOffer2)
    risc1 = 1 - (u1 / u2)
    risc2 = 1 - (u3 / u4)

    paid1, paid2 = 0, 0
    for x in lastOffer1[0][1]:
        paid1 += utility2[x]
    for x in lastOffer2[0][0]:
        paid2 += utility1[x]

    #who should make the new offer
    if risc1 < risc2:
        player = 1
        offer = calculateOffer(utility1, utility2, lastOffer1, 1, paid1)
    elif risc1 > risc2:
        player = 2
        offer = calculateOffer(utility2, utility1, lastOffer2, 2, paid2)
    else:
        player = random.randint(1,2)
        if player == 1:
            offer = calculateOffer(utility1, utility2, lastOffer1, 1, paid1)
        else:
            offer = calculateOffer(utility2, utility1, lastOffer2, 2, paid2)

    #create and return the offer
    offer = (offer, player)
    return offer


def zeuthen(utility1, utility2):

    lowest1 = (utility1[0], 0) #(utility, index)
    lowest2 = (utility2[0], 0)
    offers = []

    #calculate first offers
    for i in range(1, len(utility1)):
        if utility1[i] < lowest1[0]:
            lowest1 = (utility1[i], i)
        elif (utility1[i] == lowest1[0]) and (utility2[i] > utility2[lowest1[1]]):
            lowest1 = (utility1[i], i)

        if utility2[i] < lowest2[0]:
            lowest2 = (utility2[i], i)
        elif (utility2[i] == lowest2[0]) and (utility1[i] > utility1[lowest2[1]]):
            lowest2 = (utility2[i], i)

    aux1, aux2 = [], []
    for i in range(0, len(utility1)):
        if i != lowest1[1]:
            aux1.append(i)
        if i != lowest2[1]:
            aux2.append(i)

    offerOf1 = ([aux1, [lowest1[1]]], 1)
    offerOf2 = ([[lowest2[1]] ,aux2], 2)
    offers.append(offerOf1)
    offers.append(offerOf2)

    #stop condition
    #utility1(offer2) >= utility1(offer1) V utility2(offer1) >= utility2(offer2)
    #u1 >= u2 V u3 >= u4
    u1, u2, u3, u4 = utilities(utility1, utility2, offerOf1, offerOf2)
    while True:

        #calculate risk and new offers
        aux = zeuthenStep(utility1, utility2, offerOf1, offerOf2)
        if aux[1] == 1:
            offerOf1 = aux
        elif aux[1] == 2:
            offerOf2 = aux

        u1, u2, u3, u4 = utilities(utility1, utility2, offerOf1, offerOf2) #calculate values of stop condition
        if ((u1 >= u2) or (u3 >= u4)): #check stop condition
            break
        offers.append(aux)

    return offers


def main():
    matrixGame = np.array([[[-1.,-1.],[-10.,0.]],[[0.,-10.],[-5.,-5.]]])
    matrixGame1 = np.array([[[2.,2.],[2.,6.]],[[3.,1.],[3.,2.]]])
    matrixGame2 = np.array([[[2.,1.],[1.,2.]],[[1.,2.],[2.,1.]]]) #no nash
    matrixGame3 = np.array([[[1.,1.],[0.,0.]],[[0.,0.],[1.,1.]]]) #bi nash
    matrixGame4 = np.array([[[3.,3.],[1.,4.]],[[4.,1.],[2.,2.]]])
    matrixGame5 = np.array([[[3,6],[1,6]],[[3,7],[2,6]]])

    ItemsUtility1 = [1, 2, 1, 2]
    ItemsUtility2 = [2, 1, 1, 1]

    print('Exercise 1.1\n')
    ne, nev = findNE(matrixGame1)   #input: np.array
    print('ne\n', ne)
    print('nev\n', nev)

    print('\nExercise 1.2\n')
    ne1 = findmixedNE(matrixGame1)  #input: np.array
    print('ne\n', ne1)

    print('\nExercise 2.1\n')
    ne2, nev2, sp2 = payPromiseE(matrixGame1) #input: np.array
    print('ne\n', ne2)
    print('nev\n', nev2)
    print('sp\n', sp2)

    offers = zeuthen(ItemsUtility1, ItemsUtility2) #input: list, list
    print('\nExercise 2.1\n')
    print('Offers:')
    for offer in offers:
        print(offer)



if __name__ == '__main__':
    main()
