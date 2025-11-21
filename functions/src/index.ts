import * as functions from 'firebase-functions';
import * as admin from 'firebase-admin';

admin.initializeApp();

const db = admin.firestore();

/**
 * Transferencia de puntos entre usuarios
 * Esta función se ejecuta con privilegios de administrador en el servidor,
 * evitando problemas de permisos con las reglas de seguridad de Firestore.
 *
 * @param fromUserId - UID del usuario que envía los puntos
 * @param toUserId - UID del usuario que recibe los puntos
 * @param points - Cantidad de puntos a transferir
 * @param description - Descripción de la transferencia
 */
export const transferPoints = functions.https.onCall(async (data, context) => {
  // Verificar que el usuario esté autenticado
  if (!context.auth) {
    throw new functions.https.HttpsError(
      'unauthenticated',
      'El usuario debe estar autenticado para transferir puntos.'
    );
  }

  const { fromUserId, toUserId, points, description } = data;

  // Validaciones
  if (!fromUserId || !toUserId || !points) {
    throw new functions.https.HttpsError(
      'invalid-argument',
      'Se requieren fromUserId, toUserId y points.'
    );
  }

  // Verificar que el usuario autenticado sea el mismo que está enviando los puntos
  if (context.auth.uid !== fromUserId) {
    throw new functions.https.HttpsError(
      'permission-denied',
      'Solo puedes transferir tus propios puntos.'
    );
  }

  if (points <= 0) {
    throw new functions.https.HttpsError(
      'invalid-argument',
      'La cantidad de puntos debe ser mayor a 0.'
    );
  }

  if (fromUserId === toUserId) {
    throw new functions.https.HttpsError(
      'invalid-argument',
      'No puedes transferir puntos a ti mismo.'
    );
  }

  try {
    // Usar una transacción de Firestore para garantizar atomicidad
    const result = await db.runTransaction(async (transaction) => {
      const fromUserRef = db.collection('users').doc(fromUserId);
      const toUserRef = db.collection('users').doc(toUserId);

      const fromUserDoc = await transaction.get(fromUserRef);
      const toUserDoc = await transaction.get(toUserRef);

      // Verificar que ambos usuarios existen
      if (!fromUserDoc.exists) {
        throw new Error('El usuario origen no existe.');
      }
      if (!toUserDoc.exists) {
        throw new Error('El usuario destino no existe.');
      }

      const fromUser = fromUserDoc.data();
      const toUser = toUserDoc.data();

      if (!fromUser || !toUser) {
        throw new Error('Error al obtener datos de usuarios.');
      }

      // Verificar que el usuario tiene suficientes puntos
      const currentPoints = fromUser.points || 0;
      if (currentPoints < points) {
        throw new Error(`Puntos insuficientes. Tienes ${currentPoints} puntos.`);
      }

      // Calcular nuevos puntos
      const newFromPoints = currentPoints - points;
      const newToPoints = (toUser.points || 0) + points;

      // Actualizar puntos de ambos usuarios
      transaction.update(fromUserRef, {
        points: newFromPoints,
        totalPointsSpent: (fromUser.totalPointsSpent || 0) + points
      });

      transaction.update(toUserRef, {
        points: newToPoints,
        totalPointsEarned: (toUser.totalPointsEarned || 0) + points
      });

      // Crear transacción de envío
      const sendTransactionRef = db.collection('transactions').doc();
      transaction.set(sendTransactionRef, {
        type: 'Sent',
        amount: points,
        fromUserId: fromUserId,
        toUserId: toUserId,
        description: description || `Transferencia a ${toUser.displayName || 'usuario'}`,
        createdAt: admin.firestore.FieldValue.serverTimestamp()
      });

      // Crear transacción de recepción
      const receiveTransactionRef = db.collection('transactions').doc();
      transaction.set(receiveTransactionRef, {
        type: 'Received',
        amount: points,
        fromUserId: fromUserId,
        toUserId: toUserId,
        description: description || `Transferencia de ${fromUser.displayName || 'usuario'}`,
        createdAt: admin.firestore.FieldValue.serverTimestamp()
      });

      return {
        success: true,
        fromUserPoints: newFromPoints,
        toUserPoints: newToPoints
      };
    });

    return result;
  } catch (error: any) {
    console.error('Error en transferencia de puntos:', error);
    throw new functions.https.HttpsError(
      'internal',
      error.message || 'Error al transferir puntos.'
    );
  }
});
